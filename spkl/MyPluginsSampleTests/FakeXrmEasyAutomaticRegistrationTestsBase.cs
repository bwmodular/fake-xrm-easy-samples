﻿using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Enums;
using FakeXrmEasy.FakeMessageExecutors;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Middleware.Crud;
using FakeXrmEasy.Middleware.Messages;
using FakeXrmEasy.Middleware.Pipeline;
using FakeXrmEasy.Plugins.PluginSteps;
using Microsoft.Xrm.Sdk;
using MyPluginsSample;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MyPluginsSampleTests
{
    public class FakeXrmEasyAutomaticRegistrationTestsBase
    {
        protected readonly IXrmFakedContext _context;
        protected readonly IOrganizationService _service;

        public FakeXrmEasyAutomaticRegistrationTestsBase()
        {
            _context = MiddlewareBuilder
                            .New()

                            // Add* -> Middleware configuration
                            .AddCrud()
                            .AddFakeMessageExecutors(Assembly.GetAssembly(typeof(AddListMembersListRequestExecutor)))
                            .AddPipelineSimulation(new PipelineOptions()
                            {
                                UseAutomaticPluginStepRegistration = true,
                                PluginAssemblies = new List<Assembly>()
                                {
                                    Assembly.GetAssembly(typeof(FollowUpPlugin))
                                },
                                CustomPluginStepDiscoveryFunction = PluginStepDiscoveryFn
                            })

                            // Use* -> Defines pipeline sequence
                            .UsePipelineSimulation()
                            .UseCrud()
                            .UseMessages()

                            .SetLicense(FakeXrmEasyLicense.RPL_1_5)
                            .Build();

            _service = _context.GetOrganizationService();
        }

        private Func<Assembly, IEnumerable<PluginStepDefinition>> PluginStepDiscoveryFn = (Assembly assembly) =>
        {
            return (from t in assembly.GetTypes()
                        let attributes = t.GetCustomAttributes(typeof(CrmPluginRegistrationAttribute), true)
                        where attributes != null && attributes.Length > 0
                        select attributes
                                    .Cast<CrmPluginRegistrationAttribute>()
                                    .Select(attribute => attribute.ToPluginStepDefinition(t))
                        ).SelectMany(pluginStep => pluginStep)
                        .AsEnumerable();
        };

    }
}
