using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core
{
    public abstract class Agent
    {
        public object LockObj;
        public bool IsActive { get; set; }
        public double CurrentPerformance { get; set; }

        public abstract Task<bool> ActivateAgent();
        public abstract Task DeactivateAgent();
        public abstract Task<List<double>> PerceiveEnvironment();
        public abstract Task Action(List<double> impulse);
        public abstract Task Reset();
    }
}
