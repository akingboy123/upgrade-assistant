﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Sarif;
using Xunit;

namespace Microsoft.DotNet.UpgradeAssistant.Analysis.Tests
{
    public class SarifAnalyzeResultWriterTests
    {
        [Fact]
        public async Task ShouldThrowIfResultsIsNull()
        {
            using var source = new CancellationTokenSource();
            var writer = new SarifAnalyzeResultWriter();

            _ = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await writer.WriteAsync(null!, null!, source.Token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ValidateSarifMetadata()
        {
            var writer = new SarifAnalyzeResultWriter();

            var analyzeResults = new List<OutputResult>
            {
                new OutputResult
                {
                    Level = OutputLevel.Error,
                    FileLocation = "some-file-path",
                    LineNumber = 1,
                    ResultMessage = "some result message",
                    RuleId = "RULE0001",
                    RuleName = "RuleName0001",
                    FullDescription = "some full description",
                    HelpUri = new Uri("https://github.com/dotnet/upgrade-assistant")
                }
            };

            var analyzeResultMap = new List<OutputResultDefinition>
            {
                new OutputResultDefinition
                {
                    Name = "some-name",
                    Version = "1.0.0",
                    InformationUri = new Uri("https://github.com/dotnet/upgrade-assistant"),
                    Results = analyzeResults.ToAsyncEnumerable()
                }
            };

            using var ms = new MemoryStream();
            await writer.WriteAsync(analyzeResultMap.ToAsyncEnumerable(), ms, CancellationToken.None).ConfigureAwait(false);

            ms.Position = 0;

            var sarifLog = SarifLog.Load(ms);
            Assert.Equal("https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json", sarifLog.SchemaUri.OriginalString);
            Assert.Equal(SarifVersion.Current, sarifLog.Version);

            var analyzeResult = analyzeResults.First();
            var rule = sarifLog.Runs[0].Tool.Driver.Rules.First();
            Assert.Equal(analyzeResult.RuleId, rule.Id);
            Assert.Equal(analyzeResult.RuleName, rule.Name);
            Assert.Equal(analyzeResult.FullDescription, rule.FullDescription.Text);
            Assert.Equal(analyzeResult.HelpUri, rule.HelpUri);
            var result = sarifLog.Runs[0].Results.First();
            Assert.Equal(analyzeResult.RuleId, result.RuleId);
            Assert.Equal(analyzeResult.ResultMessage, result.Message.Text);
            Assert.Equal(FailureLevel.Error, result.Level);
        }

        [Fact]
        public async Task ValidateSarifRuleWhenFullDescriptionIsEmpty()
        {
            using var source = new CancellationTokenSource();
            var writer = new SarifAnalyzeResultWriter();

            // This result is similar to the result generated by roslyn analyzers.
            var analyzeResults = new List<OutputResult>
            {
                new OutputResult
                {
                    FileLocation = "some-file-path",
                    LineNumber = 1,
                    ResultMessage = "some result message",
                    RuleId = "RULE0001",
                    RuleName = "RuleName0001",
                    HelpUri = new Uri("https://github.com/dotnet/upgrade-assistant")
                },
                new OutputResult
                {
                    FileLocation = "some-file-path-2",
                    LineNumber = 1,
                    ResultMessage = "some result message",
                    RuleId = "RULE0002",
                    RuleName = "RuleName0002",
                    HelpUri = new Uri("https://github.com/dotnet/upgrade-assistant")
                }
            };

            var analyzeResultMap = new List<OutputResultDefinition>
            {
                new OutputResultDefinition
                {
                    Name = "some-name",
                    Version = "1.0.0",
                    InformationUri = new Uri("https://github.com/dotnet/upgrade-assistant"),
                    Results = analyzeResults.ToAsyncEnumerable()
                }
            };

            using var ms = new MemoryStream();
            await writer.WriteAsync(analyzeResultMap.ToAsyncEnumerable(), ms, source.Token).ConfigureAwait(false);

            ms.Position = 0;

            var sarifLog = SarifLog.Load(ms);
            Assert.Equal("https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json", sarifLog.SchemaUri.OriginalString);
            Assert.Equal(SarifVersion.Current, sarifLog.Version);

            Assert.Equal(analyzeResults.Count, sarifLog.Runs[0].Tool.Driver.Rules.Count);

            var firstAnalyzeResult = analyzeResults.First();
            var firstRule = sarifLog.Runs[0].Tool.Driver.Rules.First();
            Assert.Equal(firstAnalyzeResult.RuleId, firstRule.Id);
            Assert.Equal(firstAnalyzeResult.RuleName, firstRule.FullDescription.Text);
            Assert.Equal(firstAnalyzeResult.HelpUri, firstRule.HelpUri);

            var lastAnalyzeResult = analyzeResults.Last();
            var lastRule = sarifLog.Runs[0].Tool.Driver.Rules.Last();
            Assert.Equal(lastAnalyzeResult.RuleId, lastRule.Id);
            Assert.Equal(lastAnalyzeResult.RuleName, lastRule.FullDescription.Text);
            Assert.Equal(lastAnalyzeResult.HelpUri, lastRule.HelpUri);
        }
    }
}
