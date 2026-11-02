using System;

namespace RhythmRehab
{
    public enum HoldOutcome { Inactive, Holding, Completed, Broken }
    public sealed class HoldNoteState
    {
        public double StartTime { get; }
        public double EndTime { get; }
        public float TargetHeight { get; }
        public HitJudgement HeadJudgement { get; }
        private readonly float tolerance;
        private readonly double heightGrace;
        private double outsideSince = -1;
        private HoldOutcome outcome = HoldOutcome.Holding;
        public HoldNoteState(double start,double end,float target,float tolerance,HitJudgement judgement,double heightGrace=.12)
        {
            if(end<=start)throw new ArgumentException("A hold must end after its head.");
            StartTime=start;EndTime=end;TargetHeight=target;this.tolerance=tolerance;
            HeadJudgement=judgement;this.heightGrace=heightGrace;
        }
        public float Progress(double time)=>(float)Math.Max(0,Math.Min(1,(time-StartTime)/(EndTime-StartTime)));
        public HoldOutcome Step(double time,float height,bool held)
        {
            if(outcome!=HoldOutcome.Holding)return outcome;
            // A release on or after the tail's scheduled end is valid.
            if(time>=EndTime)
                return outcome=outsideSince>=0&&EndTime-outsideSince>=heightGrace?HoldOutcome.Broken:HoldOutcome.Completed;
            if(!held)return outcome=HoldOutcome.Broken;
            if(Math.Abs(height-TargetHeight)>tolerance)
            {
                if(outsideSince<0)outsideSince=time;
                if(time-outsideSince>=heightGrace)return outcome=HoldOutcome.Broken;
            }
            else outsideSince=-1;
            return outcome;
        }
    }
}
