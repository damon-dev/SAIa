using Core;
using OpenAI.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CLI
{
    public class CartpoleAgent : Agent
    {
        private ApiService aiGym;
        private string environmentId;
        private double[] currentObservation;
        private int[] currentAction;
        private bool firstPerception;
        public bool IsRendering { get; set; }

        public CartpoleAgent()
        {
            LockObj = new object();
            aiGym = new ApiService();
            IsActive = false;
            IsRendering = false;
            CurrentPerformance = 0;
        }

        public override async Task<bool> ActivateAgent()
        {
            try
            {
                if (string.IsNullOrEmpty(environmentId))
                {
                    var env = await aiGym.EnvCreate("CartPole-v0");
                    environmentId = env.InstanceID;
                }

                var initialState = await aiGym.EnvReset<double[]>(environmentId);
                currentObservation = initialState.Observation;
                CurrentPerformance = 0;
                currentAction = new int[] { 0 };
                firstPerception = true;
                IsActive = true;

                return true;
            }
            catch (Exception)
            {
                await DeactivateAgent();
                return false;
            }
        }

        public override async Task DeactivateAgent()
        {
            try
            {
                var resp = await aiGym.EnvClose(environmentId);
            }
            catch (Exception)
            {

            }

            IsActive = false;
            environmentId = null;
            currentObservation = null;
            CurrentPerformance = 0;
            currentAction = null;
            firstPerception = false;
        }

        public override async Task<List<double>> PerceiveEnvironment()
        {
            try
            {
                if (!firstPerception)
                {
                    var stepTaken = await aiGym.EnvStep<double[]>(environmentId, currentAction[0], IsRendering);

                    IsActive = !stepTaken.IsDone;
                    CurrentPerformance += stepTaken.Reward;
                    currentObservation = stepTaken.Observation;
                }
                firstPerception = false;
                return currentObservation.ToList();
            }
            catch
            {
                return currentObservation?.ToList();
            }
        }

        public override Task Action(List<double> impulse)
        {
            currentAction = new int[] { (int)Math.Clamp(Math.Round(impulse[0]), 0, 1) };
            return Task.CompletedTask;
        }

        public override async Task Reset()
        {
            try
            {
                IsActive = false;
                var initialState = await aiGym.EnvReset<double[]>(environmentId);
                currentObservation = initialState.Observation;
                CurrentPerformance = 0;
                currentAction = null;
                firstPerception = false;
            }
            catch (Exception)
            {
                await DeactivateAgent();
            }
        }
    }
}
