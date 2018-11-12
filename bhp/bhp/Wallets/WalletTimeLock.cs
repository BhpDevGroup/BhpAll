using System;

namespace Bhp.Network.RPC
{
    public class WalletTimeLock
    {
        private int Duration = 0; // minutes 
        private DateTime UnLockTime;
        private string Password;
        private bool IsAutoLock;

        public WalletTimeLock(string password, bool isAutoLock)
        {
            UnLockTime = DateTime.Now;
            Duration = 0;
            Password = password;
            IsAutoLock = isAutoLock;
        }

        public void SetPassword(string password, bool isAutoLock)
        {
            Password = password;
            IsAutoLock = isAutoLock;
        }

        /// <summary>
        /// Unlock wallet
        /// </summary>
        /// <param name="Duration">Unlock duration</param>
        public bool UnLock(string password, int duration)
        {
            lock (this)
            {
                if (Password.Length > 0 && Password.Equals(password))
                {
                    Duration = duration > 1 ? duration : 1;
                    UnLockTime = DateTime.Now;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsLocked()
        {
            if (IsAutoLock == false)
            {
                return false;
            }

            lock (this)
            {
                TimeSpan span = new TimeSpan(DateTime.Now.Ticks) - new TimeSpan(UnLockTime.Ticks);
                return ((int)span.TotalMinutes >= Duration);
            }
        }
    }
}
