// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Settings.cs" company="West Bend">
//   Copyright (c) 2020 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga
{
    using System.Configuration;

    public class Settings
    {
        private static string apimURL = null;

        private static string apimSubscriptionKey = null;

        public static string ApimURL
        {
            get
            {
                return Settings.apimURL ?? (Settings.apimURL = ConfigurationManager.AppSettings["ApimUrL"]);
            }

            set
            {
                Settings.apimURL = value;
            }
        }

        public static string ApimSubscriptionKey
        {
            get
            {
                return Settings.apimSubscriptionKey ?? (Settings.apimSubscriptionKey = ConfigurationManager.AppSettings["ApimSubscriptionKey"]);
            }

            set
            {
                Settings.apimSubscriptionKey = value;
            }
        }
    }
}
