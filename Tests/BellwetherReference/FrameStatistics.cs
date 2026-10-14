using System;
using System.Collections.Generic;
using System.Linq;
namespace HollowCurrent
{
    [Serializable]
    public sealed class FrameStatistics
    {
        public int frames,invalid_samples,frames_over_16ms,frames_over_33ms;
        public float mean_fps,minimum_frame_fps,minimum_one_second_fps,one_percent_low_fps;
        public float p50_ms,p95_ms,p99_ms,slowest_frame_ms;
        public bool meets_60_fps_floor;
        public static FrameStatistics Measure(IReadOnlyList<float> samples)
        {
            var result=new FrameStatistics();var valid=new List<float>(samples.Count);
            foreach(float value in samples)
            {if(value<=0||float.IsNaN(value)||float.IsInfinity(value)){result.invalid_samples++;continue;}valid.Add(value);}
            result.frames=valid.Count;if(valid.Count==0)return result;
            double total=0,window=0;int left=0;float minimumWindow=float.PositiveInfinity;
            for(int right=0;right<valid.Count;right++)
            {
                float dt=valid[right];total+=dt;window+=dt;
                if(dt>16.667f)result.frames_over_16ms++;if(dt>33.333f)result.frames_over_33ms++;
                while(left<right&&window-valid[left]>=1000)window-=valid[left++];
                if(window>=1000)minimumWindow=Math.Min(minimumWindow,(float)((right-left+1)*1000/window));
            }
            result.mean_fps=(float)(valid.Count*1000/total);
            result.minimum_one_second_fps=float.IsPositiveInfinity(minimumWindow)?0:minimumWindow;
            valid.Sort();float P(double q)=>valid[Math.Max(0,Math.Min(valid.Count-1,(int)Math.Ceiling(valid.Count*q)-1))];
            result.p50_ms=P(.5);result.p95_ms=P(.95);result.p99_ms=P(.99);result.slowest_frame_ms=valid[valid.Count-1];
            result.minimum_frame_fps=1000/result.slowest_frame_ms;
            int tail=Math.Max(1,(int)Math.Ceiling(valid.Count*.01));double tailTotal=0;
            for(int i=valid.Count-tail;i<valid.Count;i++)tailTotal+=valid[i];
            result.one_percent_low_fps=(float)(tail*1000/tailTotal);
            result.meets_60_fps_floor=valid.Count>=3000&&result.invalid_samples==0&&result.frames_over_16ms==0;
            return result;
        }
    }
}
