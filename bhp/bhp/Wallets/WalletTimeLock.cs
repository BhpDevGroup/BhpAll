using Bhp.Wallets;
using System;
using System.Threading;

namespace Bhp.Network.RPC
{
    public class WalletTimeLock
    {
        private int Duration = 0; // minutes 
        private DateTime UnLockTime;        
        private bool IsAutoLock;
        private ReaderWriterLockSlim rwlock;

        public WalletTimeLock(bool isAutoLock)
        {
            UnLockTime = DateTime.Now;
            Duration = 0; 
            IsAutoLock = isAutoLock;
            rwlock = new ReaderWriterLockSlim();
        }

        public void SetAutoLock(bool isAutoLock)
        {
            IsAutoLock = isAutoLock;
        }

        /// <summary>
        /// Unlock wallet
        /// </summary>
        /// <param name="Duration">Unlock duration</param>
        public bool UnLock(Wallet wallet, string password, int duration)
        {
            bool unlock = false;
            try
            {
                rwlock.EnterWriteLock();
                if (wallet.VerifyPassword(password))
                {
                    Duration = duration > 1 ? duration : 1;
                    UnLockTime = DateTime.Now;
                    unlock = true;
                }
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
            return unlock;
        }

        public bool IsLocked()
        {
            if (IsAutoLock == false)
            {
                return false;
            }

            //wallet is locked by default.
            bool locked = true;
            try
            {
                rwlock.EnterReadLock();
                TimeSpan span = new TimeSpan(DateTime.Now.Ticks) - new TimeSpan(UnLockTime.Ticks);
                locked = ((int)span.TotalMinutes >= Duration);
            }
            finally
            {
                rwlock.ExitReadLock();
            }
            return locked;
        }
    }
}
