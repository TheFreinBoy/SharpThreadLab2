using System.Diagnostics;
using System.Text;

namespace SharpThreadLab2
{
    public class ArrClass
    {
        private readonly int dim;
        private readonly int[] arr;

        private int globalMin;
        private int globalMinIndex;
        
        private readonly object lockerForMin = new object();
        private readonly object lockerForCount = new object();
        
        private int threadCount;
        private int currentThreadNum;

        public ArrClass(int dim)
        {
            this.dim = dim;
            this.arr = new int[dim];
            InitArr();
        }

        private void InitArr()
        {
            Console.WriteLine($"Генерація масиву з {dim} елементів... (зачекайте)");
            Random rnd = new Random();
            
            for (int i = 0; i < dim; i++)
            {
                arr[i] = rnd.Next(1, 101); 
            }

            int randomIndex = rnd.Next(0, dim);
            int randomNegative = -(rnd.Next(1, 1001)); 
            arr[randomIndex] = randomNegative;

            Console.WriteLine($"[ЗГЕНЕРОВАНО] Мінусовий елемент: {randomNegative}, з індексом: {randomIndex}\n");
        }

        public void ResetForNewSearch(int threadNum)
        {
            this.globalMin = int.MaxValue;
            this.globalMinIndex = -1;
            this.threadCount = 0;
            this.currentThreadNum = threadNum;
        }

        public (int min, int index) FindPartMin(int startIndex, int finishIndex)
        {
            int localMin = int.MaxValue;
            int localMinIndex = -1;

            for (int i = startIndex; i < finishIndex; i++)
            {
                if (arr[i] < localMin)
                {
                    localMin = arr[i];
                    localMinIndex = i;
                }
            }
            return (localMin, localMinIndex);
        }

        public void CollectMin(int localMin, int localMinIndex)
        {
            bool acquired = false;
            try
            {
                Monitor.Enter(lockerForMin, ref acquired);
                if (localMin < globalMin)
                {
                    globalMin = localMin;
                    globalMinIndex = localMinIndex;
                }
            }
            finally
            {
                if (acquired) Monitor.Exit(lockerForMin);
            }
        }

        public void IncThreadCount()
        {
            bool acquired = false;
            try
            {
                Monitor.Enter(lockerForCount, ref acquired);
                threadCount++;
                Monitor.Pulse(lockerForCount);
            }
            finally
            {
                if (acquired) Monitor.Exit(lockerForCount);
            }
        }

        private void WaitAllThreads()
        {
            bool acquired = false;
            try
            {
                Monitor.Enter(lockerForCount, ref acquired);
                while (threadCount < currentThreadNum)
                {
                    Monitor.Wait(lockerForCount);
                }
            }
            finally
            {
                if (acquired) Monitor.Exit(lockerForCount);
            }
        }

        public void ParallelMin()
        {
            int chunkSize = dim / currentThreadNum;
            int remainder = dim % currentThreadNum;
            int currentStart = 0;

            for (int i = 0; i < currentThreadNum; i++)
            {
                int currentFinish = currentStart + chunkSize + (i < remainder ? 1 : 0);

                ThreadMin worker = new ThreadMin(currentStart, currentFinish, this);
                Thread t = new Thread(worker.Run);
                t.Start();

                currentStart = currentFinish;
            }

            WaitAllThreads();
        }

        public int GetGlobalMin() => globalMin;
        public int GetGlobalMinIndex() => globalMinIndex;
    }

    public class ThreadMin
    {
        private readonly int startIndex;
        private readonly int finishIndex;
        private readonly ArrClass arrClass;

        public ThreadMin(int startIndex, int finishIndex, ArrClass arrClass)
        {
            this.startIndex = startIndex;
            this.finishIndex = finishIndex;
            this.arrClass = arrClass;
        }

        public void Run()
        {
            var result = arrClass.FindPartMin(startIndex, finishIndex);
            arrClass.CollectMin(result.min, result.index);
            arrClass.IncThreadCount();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int dim = 1000000000; 
            
            ArrClass arrClass = new ArrClass(dim);
            int[] threadConfigs = { 1, 2, 4, 6, 8 };

            Console.WriteLine("--- ПОЧАТОК ПОШУКУ (C#) ---");

            foreach (int threads in threadConfigs)
            {
                arrClass.ResetForNewSearch(threads);

                Stopwatch sw = Stopwatch.StartNew();
                arrClass.ParallelMin();
                sw.Stop();

                Console.WriteLine($"Потоків: {threads} | Час: {sw.ElapsedMilliseconds,4} мс | Знайдено мін: {arrClass.GetGlobalMin()} (індекс: {arrClass.GetGlobalMinIndex()})");
            }

            Console.ReadKey();
        }
    }
}