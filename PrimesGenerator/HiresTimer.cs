using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrimesGenerator
{
    internal class HiresTimer : IDisposable
    {
        private int _fileDescriptor;
        private byte[] _buf;
        private GCHandle _handle;
        private IntPtr _pointer;

        public HiresTimer()
        {
            _fileDescriptor = timerfd_create(ClockIds.CLOCK_MONOTONIC, 0);
            _buf = new byte[8];
            _handle = GCHandle.Alloc(_buf, GCHandleType.Pinned);
            _pointer = _handle.AddrOfPinnedObject();
        }

        public void Wait(TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero) 
            {
                return;
            }
            uint sec = (uint)timeout.TotalSeconds;
            uint ns = (uint)((timeout.Ticks % TimeSpan.TicksPerSecond) * TimeSpan.NanosecondsPerTick);
            var itval = new itimerspec
            {
                it_interval = new timespec
                {
                    tv_sec = 0,
                    tv_nsec = 0
                },
                it_value = new timespec
                {

                    tv_sec = sec,
                    tv_nsec = ns
                }
            };

            int ret = timerfd_settime(_fileDescriptor, 0, itval, null);
            if (ret != 0)
                throw new Exception($"Error from timerfd_settime = {ret}");

            ret = read(_fileDescriptor, _pointer, _buf.Length);

            if (ret < 0)
                throw new Exception($"Error in read = {ret}");

        }

        public void Dispose()
        {
            close(_fileDescriptor);
            _handle.Free();
        }

        enum ClockIds : int
        {
            CLOCK_REALTIME = 0,
            CLOCK_MONOTONIC = 1,
            CLOCK_PROCESS_CPUTIME_ID = 2,
            CLOCK_THREAD_CPUTIME_ID = 3,
            CLOCK_MONOTONIC_RAW = 4,
            CLOCK_REALTIME_COARSE = 5,
            CLOCK_MONOTONIC_COARSE = 6,
            CLOCK_BOOTTIME = 7,
            CLOCK_REALTIME_ALARM = 8,
            CLOCK_BOOTTIME_ALARM = 9
        }
        [StructLayout(LayoutKind.Explicit)]
        class timespec
        {
            [FieldOffset(0)]
            public ulong tv_sec;                 /* seconds */
            [FieldOffset(8)]
            public ulong tv_nsec;                /* nanoseconds */
        };
        [StructLayout(LayoutKind.Explicit)]
        class itimerspec
        {
            [FieldOffset(0)]
            public timespec it_interval;    /* timer period */

            [FieldOffset(16)]
            public timespec it_value;       /* timer expiration */
        };
        [DllImport("libc")]
        static extern int timerfd_create(ClockIds clockId, int flags);

        [DllImport("libc")]
        static extern int timerfd_settime(int fd, int flags, itimerspec new_value, itimerspec? old_value);

        [DllImport("libc", SetLastError = true)]
        static extern int read(int fd, IntPtr buf, int count);

        [DllImport("libc")]
        static extern int close(int fd);

    }
}
