using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace SonwFlake
{
    public class SnowFlake
    {
        /**
         * Start time cut (2020-05-03)
         */
        private const long twepoch = 1588435200000L;

        /**
         * The number of bits occupied by workerId
         */
        private const int workerIdBits = 10;

        /**
         * The number of bits occupied by timestamp
         */
        private const int timestampBits = 41;

        /**
         * The number of bits occupied by sequence
         */
        private const int sequenceBits = 12;

        /**
         * Maximum supported machine id, the result is 1023
         */
        private const int maxWorkerId = ~(-1 << workerIdBits);

        /**
         * business meaning: machine ID (0 ~ 1023)
         * actual layout in memory:
         * highest 1 bit: 0
         * middle 10 bit: workerId
         * lowest 53 bit: all 0
         */
        private long? workerId;

        /**
         * timestamp and sequence mix in one Long
         * highest 11 bit: not used
         * middle  41 bit: timestamp
         * lowest  12 bit: sequence
         */
        private long timestampAndSequence;

        /**
         * mask that help to extract timestamp and sequence from a long
         */
        private const long timestampAndSequenceMask = ~(-1L << (timestampBits + sequenceBits));


        private static SnowFlake _instance;

        private static readonly object SLock = new object();

        /**
         * instantiate an IdWorker using given workerId
         * @param workerId if null, then will auto assign one
         */
        private SnowFlake(long? workerId)
        {
            InitTimestampAndSequence();
            InitSnowFlake(workerId);
        }

        public static SnowFlake GetInstance(long? workerId)
        {
            if (_instance != null)
            {
                return _instance;
            }
            else
            {
                lock (SLock)
                {
                    if (_instance != null)
                    {
                        return _instance;
                    }

                    return _instance = new SnowFlake(workerId);
                }
            }
        }

        /// <summary>
        /// get next UUID(base on snowflake algorithm), which look like:
        /// highest 1 bit: always 0
        /// next   10 bit: workerId
        /// next   41 bit: timestamp
        /// lowest 12 bit: sequence
        /// </summary>
        /// <returns></returns>
        public long NextId()
        {
            WaitIfNecessary();
            long next = this.timestampAndSequence;
            long timestampWithSequence = next & timestampAndSequenceMask;
            return (long)(this.workerId | timestampWithSequence);
        }

        /// <summary>
        /// block current thread if the QPS of acquiring UUID is too high
        /// that current sequence space is exhausted
        /// </summary>
        private void WaitIfNecessary()
        {
            long currentWithSequence = Interlocked.Increment(ref this.timestampAndSequence);
            long current = currentWithSequence >> sequenceBits;
            long newest = GetNewestTimestamp();

            if (current >= newest)
            {
                Thread.Sleep(5);
            }
        }

        private void InitSnowFlake(long? workerId)
        {
            if (workerId == null)
            {
                workerId = GenerateWorkerId();
            }

            if (workerId > maxWorkerId || workerId < 0)
            {
                String message = String.Format("worker Id can't be greater than %d or less than 0", maxWorkerId);
                throw new ArgumentException(message);
            }

            this.workerId = workerId << (timestampBits + sequenceBits);
        }


        /// <summary>
        /// auto generate workerId, try using mac first, if failed, then randomly generate one
        /// </summary>
        /// <returns>workerId</returns>
        private long GenerateWorkerId()
        {
            try
            {
                return GenerateWorkerIdBaseOnMac();
            }
            catch (Exception e)
            {
                return GenerateRandomWorkerId();
            }
        }

        /// <summary>
        /// init first timestamp and sequence immediately
        /// </summary>
        private void InitTimestampAndSequence()
        {
            long timestamp = GetNewestTimestamp();
            long timestampWithSequence = timestamp << sequenceBits;
            this.timestampAndSequence = timestampWithSequence;
        }

        /// <summary>
        /// get newest timestamp relative to twepoch
        /// </summary>
        /// <returns></returns>
        private long GetNewestTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - twepoch;
        }

        /// <summary>
        /// use lowest 10 bit of available MAC as workerId
        /// </summary>
        /// <returns>workerId</returns>
        private long GenerateWorkerIdBaseOnMac()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            //Console.WriteLine("Interface information for {0}.{1}     ",
            //computerProperties.HostName, computerProperties.DomainName);

            if (nics == null || nics.Length < 1)
            {
                throw new Exception("no available mac found");
            }

            var adapter = nics.FirstOrDefault();
            PhysicalAddress address = adapter.GetPhysicalAddress();
            byte[] mac = address.GetAddressBytes();

            return ((mac[4] & 0B11) << 8) | (mac[5] & 0xFF);
        }

        /// <summary>
        /// randomly generate one as workerId
        /// </summary>
        /// <returns></returns>
        private long GenerateRandomWorkerId()
        {
            return new Random().Next(maxWorkerId + 1);
        }
    }
}
