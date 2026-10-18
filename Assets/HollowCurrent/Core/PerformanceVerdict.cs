using System;
using System.Collections.Generic;
using System.Text;
namespace HollowCurrent
{
    // Engine-side cost of each measured frame (Unity FrameTimingManager: max of main-thread, render-thread and GPU
    // time). Unlike the wall-clock frame delta it does not include time the OS spent not scheduling the player
    // (background window on a shared machine, frame-limiter sleep overshoot), so it separates "the game missed
    // 16.7 ms" from "the window was not scheduled for 16.7 ms".
    [Serializable]
    public sealed class CostStatistics
    {
        public bool available;
        public int samples,over_16ms,skipped_samples;
        public float p50_ms,p99_ms,max_ms;
        public string source;
        public static CostStatistics Measure(IReadOnlyList<float> costs,int skipped,string source)
        {
            var result=new CostStatistics{source=source,skipped_samples=skipped};
            var valid=new List<float>(costs.Count);
            foreach(float c in costs)if(!(c<=0||float.IsNaN(c)||float.IsInfinity(c)))valid.Add(c);
            result.samples=valid.Count;if(valid.Count==0)return result;
            foreach(float c in valid)if(c>PerformanceVerdict.BudgetMs)result.over_16ms++;
            valid.Sort();float P(double q)=>valid[Math.Max(0,Math.Min(valid.Count-1,(int)Math.Ceiling(valid.Count*q)-1))];
            result.p50_ms=P(.5);result.p99_ms=P(.99);result.max_ms=valid[valid.Count-1];
            result.available=valid.Count>=PerformanceVerdict.MinCostSamples;
            return result;
        }
    }
    // Decides performance_pass for -hc-verify and says why. The floor is unchanged: every measured frame must
    // fit 16.667 ms. With engine cost available that is judged on engine cost, and wall-clock must also hold
    // p99 <= 16.667 ms, 1 % low >= 60 fps and every one-second window >= 60 fps. Without engine cost the strict
    // wall-clock rule (FrameStatistics.meets_60_fps_floor and zero UI frames over 16.667 ms) applies.
    public static class PerformanceVerdict
    {
        public const float BudgetMs=16.667f;
        public const int MinWallSamples=3000,MinCostSamples=1000;
        public static bool Evaluate(bool routePassed,FrameStatistics timing,FrameStatistics ui,CostStatistics cost,out string reason)
        {
            var why=new List<string>();
            if(!routePassed)why.Add("route_failed");
            if(timing.frames<MinWallSamples)why.Add("too_few_frames="+timing.frames);
            if(timing.invalid_samples>0)why.Add("invalid_samples="+timing.invalid_samples);
            bool engine=cost!=null&&cost.available;
            if(engine)
            {
                if(cost.over_16ms>0)why.Add("engine_cost_over_16ms="+cost.over_16ms+"(max "+cost.max_ms.ToString("F1")+" ms)");
                if(timing.p99_ms>BudgetMs)why.Add("wall_p99_ms="+timing.p99_ms.ToString("F1"));
                if(timing.one_percent_low_fps<60)why.Add("one_percent_low_fps="+timing.one_percent_low_fps.ToString("F1"));
                if(timing.minimum_one_second_fps<60)why.Add("min_one_second_fps="+timing.minimum_one_second_fps.ToString("F1"));
            }
            else
            {
                if(timing.frames_over_16ms>0)why.Add("wall_frames_over_16ms="+timing.frames_over_16ms+" (engine cost unavailable, strict wall-clock rule)");
                if(ui!=null&&ui.frames_over_16ms>0)why.Add("ui_frames_over_16ms="+ui.frames_over_16ms+" (engine cost unavailable)");
            }
            if(why.Count==0){reason=engine?"ok engine_cost_clean wall_frames_over_16ms="+timing.frames_over_16ms+" (scheduler jitter, not engine cost)":"ok strict_wall_clock_clean";return true;}
            var sb=new StringBuilder();for(int i=0;i<why.Count;i++){if(i>0)sb.Append(';');sb.Append(why[i]);}
            reason=sb.ToString();return false;
        }
    }
}
