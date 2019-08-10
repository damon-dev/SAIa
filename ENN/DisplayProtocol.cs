using System;
using System.Collections.Generic;

namespace EvolutionalNeuralNetwork
{
    public enum Response { Quit, Load, Train, Stop, Ignore }

    public class DisplayProtocol
    {
        protected List<List<double>> input;
        protected List<List<double>> expectedOutput;

        public DisplayProtocol()
        {
            Console.CursorVisible = false;
        }

        public virtual void Display(Entity champion)
        {
            if (champion == null)
            {
                Console.WriteLine("T: Train L: Load S: Stop Q: Quit");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            else
            {
                Console.WriteLine("T: Train S: Stop Q: Quit        ");
                Console.WriteLine($"Last updated: {DateTime.Now.ToLocalTime()}");
            }
        }

        public Response ReadResponse()
        {
            var resp = Console.ReadKey(true);

            switch(resp.KeyChar)
            {
                // Quit
                case 'q':
                case 'Q':
                    return Response.Quit;

                // Load
                case 'l':
                case 'L':

                    return Response.Load;

                // Train
                case 't':
                case 'T':

                    return Response.Train;

                // Stop
                case 's':
                case 'S':

                    return Response.Stop;

                default:

                    return Response.Ignore;
            }
        }
    }
}
