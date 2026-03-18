
namespace SharpThreadLab2
{
    class Program
    {
        private static readonly int dim = 10000000;
        private static readonly int threadNum = 2; 

        private readonly Thread[] threads = new Thread[threadNum];
        private readonly int[] arr = new int[dim];
        
        private int globalMin = int.MaxValue;
        private int globalMinIndex = -1;
        private readonly object lockerForMin = new object();
        
        private int threadCount = 0;
        private readonly object lockerForCount = new object();

        static void Main(string[] args)
        {
            Program main = new Program();
            
            main.InitArr();
            
            main.ParallelMin();

            Console.ReadKey();
        }

        private void InitArr()
        {
            Random rnd = new Random();
            
            for (int i = 0; i < dim; i++)
            {
                arr[i] = i;
            }

            int randomIndex = rnd.Next(0, dim);
            arr[randomIndex] = -99999;
            
            Console.WriteLine($"[Генерація] Від'ємне число (-99999) розміщено під індексом: {randomIndex}\n");
        }

        class Bound
        {
            public int StartIndex { get; set; }
            public int FinishIndex { get; set; }

            public Bound(int startIndex, int finishIndex)
            {
                StartIndex = startIndex;
                FinishIndex = finishIndex;
            }
        }

        private void ParallelMin()
        {
            int chunkSize = dim / threadNum;
            int remainder = dim % threadNum;

            int currentStart = 0;

            for (int i = 0; i < threadNum; i++)
            {
                int currentFinish = currentStart + chunkSize + (i < remainder ? 1 : 0);

                threads[i] = new Thread(StarterThread);
                threads[i].Start(new Bound(currentStart, currentFinish));

                currentStart = currentFinish; 
            }
            
            lock (lockerForCount)
            {
                while (threadCount < threadNum)
                {
                    Monitor.Wait(lockerForCount);
                }
            }
            
            Console.WriteLine($"[Результат] Мінімальний елемент: {globalMin}, Індекс: {globalMinIndex}");
        }

        private void StarterThread(object param)
        {
            if (param is Bound bound)
            {
                int localMin = int.MaxValue;
                int localMinIndex = -1;
                
                for (int i = bound.StartIndex; i < bound.FinishIndex; i++)
                {
                    if (arr[i] < localMin)
                    {
                        localMin = arr[i];
                        localMinIndex = i;
                    }
                }
                
                lock (lockerForMin)
                {
                    if (localMin < globalMin)
                    {
                        globalMin = localMin;
                        globalMinIndex = localMinIndex;
                    }
                }

                IncThreadCount();
            }
        }

        private void IncThreadCount()
        {
            lock (lockerForCount)
            {
                threadCount++;
                Monitor.Pulse(lockerForCount); 
            }
        }
    }
}