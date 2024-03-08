using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Smaug
{
    class NetworkShares
    {
        public string HostName { get; } = null;
        public string ShareName { get; } = null;
        public string Description { get; } = null;

        private NetworkShares(string hostname, string netname, string remark)
        {
            this.HostName = hostname;
            this.ShareName = netname;
            this.Description = remark;
        }

        public override string ToString()
        {
            return string.Format("\\\\{0}\\{1}", this.HostName, this.ShareName);
        }

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetShareEnum(string servername, uint level, out IntPtr bufptr, uint prefmaxlen, out int entriesread, out int totalentries, ref int resume_handle);

        [DllImport("Netapi32.dll", SetLastError = true)]
        private static extern int NetApiBufferFree(IntPtr Buffer);

        private static uint STYPE_MASK = 0xff;
        private static uint STYPE_SPECIAL = 0x80000000;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHARE_INFO_1
        {
            public string shi1_netname;
            public uint shi1_type;
            public string shi1_remark;
        }

        public static IEnumerable<NetworkShares> EnumerateShares(string hostname)
        {
            int resume_handle = 0;

            if (NetShareEnum(hostname, 1, out IntPtr bufptr, 0xFFFFFFFF, out int entriesread, out int totalentries, ref resume_handle) == 0)
            {
                var current_ptr = bufptr;

                for (int i = 0; i < entriesread; i++)
                {
                    var shi = Marshal.PtrToStructure<SHARE_INFO_1>(current_ptr);

                    // Ignore (disktree, printq, device, ipc) and (special / built-in) shares
                    if ((shi.shi1_type & STYPE_MASK) == 0 && (shi.shi1_type & STYPE_SPECIAL) == 0)
                        yield return new NetworkShares(hostname, shi.shi1_netname, shi.shi1_remark);

                    current_ptr = IntPtr.Add(current_ptr, Marshal.SizeOf<SHARE_INFO_1>());
                }

                NetApiBufferFree(bufptr);
            }
        }
    }
}
