using Core;
using OpenAI.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CLI
{
    public class Display
    {
        private ApiService apiService;

        private DateTime latestTime;
        private Entity champion;
        public Entity Champion
        {
            get { return champion; }
            set { champion = value; latestTime = DateTime.Now; }
        }
        public bool KeepAnimating { get; set; }

        public Display()
        {
            apiService = new ApiService();
        }

        public async Task Animate()
        {
            KeepAnimating = true;
            var agent = new CartpoleAgent();
            agent.IsRendering = true;
            try
            {
                while (KeepAnimating)
                {
                    if (Champion == null)
                        await Task.Delay(1000);
                    else
                    {
                        var brain = new Brain();
                        brain.GenerateFromStructure(Champion.Genes);
                        await agent.ActivateAgent();
                        await brain.Hijack(agent);
                        Console.WriteLine($"Latest Improvement: {latestTime}      " );
                        Console.WriteLine($"Best Fitness: {Champion.Fitness:0.00}      ");
                        Console.WriteLine($"Fitness: {agent.CurrentPerformance:0.00}      ");
                        Console.SetCursorPosition(0, Console.CursorTop - 3);
                    }
                }
            }
            catch
            {
                Champion = null;
            }
            await agent.DeactivateAgent();
        }
    }
}
