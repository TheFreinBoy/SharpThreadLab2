using System.Text;
namespace SharpThreadLab2
{
    public class ArrClass
    {
        private readonly int dim;
        private readonly int threadNum;
        private readonly int[] arr;

        private int globalMin = int.MaxValue;
        private int globalMinIndex = -1;
        private readonly object lockerForMin = new object();

        private int threadCount = 0;
        private readonly object lockerForCount = new object();

        public ArrClass(int dim, int threadNum)
        {
            this.dim = dim;
            this.threadNum = threadNum;
            this.arr = new int[dim];
            InitArr();
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

            Console.WriteLine($"[Генерація] Від'ємне число (-99999) розміщено під індексом: {randomIndex}");
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
                if (acquired)
                {
                    Monitor.Exit(lockerForMin);
                }
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
                if (acquired)
                {
                    Monitor.Exit(lockerForCount);
                }
            }
        }
        
        private void WaitAllThreads()
        {
            bool acquired = false;
            try
            {
                Monitor.Enter(lockerForCount, ref acquired);
                while (threadCount < threadNum)
                {
                    Monitor.Wait(lockerForCount);
                }
            }
            finally
            {
                if (acquired)
                {
                    Monitor.Exit(lockerForCount);
                }
            }
        }

        public void ParallelMin()
        {
            int chunkSize = dim / threadNum;
            int remainder = dim % threadNum;
            int currentStart = 0;

            for (int i = 0; i < threadNum; i++)
            {
                int currentFinish = currentStart + chunkSize + (i < remainder ? 1 : 0);

                ThreadMin worker = new ThreadMin(currentStart, currentFinish, this);
                Thread t = new Thread(worker.Run);
                t.Start();

                currentStart = currentFinish;
            }

            WaitAllThreads();

            Console.WriteLine($"\n[Результат] Мінімальний елемент: {globalMin}");
            Console.WriteLine($"[Результат] Індекс: {globalMinIndex}");
        }
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
            Console.OutputEncoding = Encoding.UTF8;
            int dim = 10000000;
            int threadNum = 2;

            ArrClass arrClass = new ArrClass(dim, threadNum);
            arrClass.ParallelMin();

            Console.ReadKey();
        }
    }
}