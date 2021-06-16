﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace PriorityQueue.Shared
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.ServiceBus;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class PriorityWorkerRole : RoleEntryPoint
    {
        private QueueManager queueManager;
        private readonly ManualResetEvent completedEvent = new ManualResetEvent(false);

        public override void Run()
        {
            // Start listening for messages on the subscription.
            var subscriptionName = CloudConfigurationManager.GetSetting("SubscriptionName");
            this.queueManager.ReceiveMessages(this.ProcessMessage);

            this.completedEvent.WaitOne();
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // Make sure you provide the corresponding Service Bus connection information in the service configuration file.
            var connectionString = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
            var subscriptionName = CloudConfigurationManager.GetSetting("SubscriptionName");
            var topicName = CloudConfigurationManager.GetSetting("TopicName");

            this.queueManager = new QueueManager(connectionString, topicName);

            // create the subscriptions, one for each priority.
            this.queueManager.Setup(subscriptionName, priority: subscriptionName);

            return base.OnStart();
        }

        public override void OnStop()
        {
            this.queueManager.StopReceiver(TimeSpan.FromSeconds(30)).Wait();

            this.completedEvent.Set();

            base.OnStop();
        }

        protected virtual async Task ProcessMessage(ServiceBusReceivedMessage message)
        {
            // simulating processing
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
