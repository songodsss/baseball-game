using System;
using UnityEngine;
#if UNITY_ANDROID && HAS_MOBILE_NOTIFICATIONS
using Unity.Notifications.Android;
#endif

namespace BaseballGame
{
    /// <summary>
    /// Reminders on the phone while the game is closed: the daily check-in, and "your player is waiting" after a day
    /// and after three. Everything is cancelled when the game comes back.
    ///
    /// This lives in its own assembly so the game builds without the notifications package: the code only switches on
    /// for Android with com.unity.mobile.notifications installed. The game talks to it through PlayerPrefs alone.
    /// </summary>
    public class PhoneNotifications : MonoBehaviour
    {
        /// <summary>1 = on (the default), 0 = off. Set from the settings screen.</summary>
        public const string EnabledKey = "notify_enabled";
        /// <summary>The current player's name, for "OO 선수가 기다리고 있어요".</summary>
        public const string PlayerKey = "notify_player";
        /// <summary>yyyy-MM-dd of the last check-in, so a checked-in day isn't nagged.</summary>
        public const string CheckInKey = "notify_checkin";

        const string Channel = "baseball_reminders";
        const int EveningHour = 19;
        const int QuietFrom = 22, QuietUntil = 9;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set { PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>True when this build can actually post notifications.</summary>
        public static bool Supported
        {
            get
            {
#if UNITY_ANDROID && HAS_MOBILE_NOTIFICATIONS
                return true;
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Supported) return;
            var go = new GameObject("PhoneNotifications");
            DontDestroyOnLoad(go);
            go.AddComponent<PhoneNotifications>();
        }

        void Start()
        {
#if UNITY_ANDROID && HAS_MOBILE_NOTIFICATIONS
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(Channel, "훈련 알림", "출석 보상과 훈련 알림", Importance.Default));
            if (Enabled) new PermissionRequest(); // Android 13+ asks once; older phones allow by default
            AndroidNotificationCenter.CancelAllScheduledNotifications();
#endif
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) ScheduleAll();
            else CancelAll();
        }

        void OnApplicationQuit() => ScheduleAll();

        static void CancelAll()
        {
#if UNITY_ANDROID && HAS_MOBILE_NOTIFICATIONS
            AndroidNotificationCenter.CancelAllScheduledNotifications();
            AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif
        }

        static void ScheduleAll()
        {
            CancelAll();
            if (!Enabled) return;
            var now = DateTime.Now;
            string name = PlayerPrefs.GetString(PlayerKey, "");

            bool checkedIn = PlayerPrefs.GetString(CheckInKey, "") == now.ToString("yyyy-MM-dd");
            var evening = now.Date.AddHours(EveningHour);
            if (checkedIn || now >= evening) evening = evening.AddDays(1);
            Send("출석 보상이 기다려요", "오늘 출석하고 보상을 받아 가세요!", evening);

            if (name.Length > 0)
            {
                Send($"{name} 선수가 기다리고 있어요", "오늘 훈련 메뉴를 정해 주세요. 컨디션이 좋은 날일지도 몰라요!", OutOfQuietHours(now.AddDays(1)));
                Send("감독님이 찾고 있어요", $"{name} 선수의 시즌이 멈춰 있어요. 다시 그라운드로!", OutOfQuietHours(now.AddDays(3)));
            }
        }

        static void Send(string title, string text, DateTime when)
        {
#if UNITY_ANDROID && HAS_MOBILE_NOTIFICATIONS
            AndroidNotificationCenter.SendNotification(new AndroidNotification { Title = title, Text = text, FireTime = when }, Channel);
#endif
        }

        /// <summary>No buzzing at night: late reminders move to noon the next day, early ones to noon the same day.</summary>
        public static DateTime OutOfQuietHours(DateTime when)
        {
            if (when.Hour >= QuietFrom) return when.Date.AddDays(1).AddHours(12);
            if (when.Hour < QuietUntil) return when.Date.AddHours(12);
            return when;
        }
    }
}
