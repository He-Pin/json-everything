﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using Json.More;

namespace Json.Schema.Benchmark.Suite;

[MemoryDiagnoser]
public class TestSuiteRunner
{
	private const string _benchmarkOffset = @"../../../../";
	private const string _testCasesPath = @"../../../../ref-repos/JSON-Schema-Test-Suite/tests";
	private const string _remoteSchemasPath = @"../../../../ref-repos/JSON-Schema-Test-Suite/remotes";

	private const bool _runDraftNext = true;

	private static (TestCollection, TestCase)[] _allTests;

	public static IEnumerable<TestCollection> GetAllTests()
	{
		return GetTests("draft6")
			.Concat(GetTests("draft7"))
			.Concat(GetTests("draft2019-09"))
			.Concat(GetTests("draft2020-12"))
			.Concat(_runDraftNext ? GetTests("draft-next") : Enumerable.Empty<TestCollection>());
	}

	private static IEnumerable<TestCollection> GetTests(string draftFolder)
	{
		// ReSharper disable once HeuristicUnreachableCode

		var testsPath = Path.Combine(Directory.GetCurrentDirectory(), _benchmarkOffset, _testCasesPath, $"{draftFolder}/")
			.AdjustForPlatform();
		if (!Directory.Exists(testsPath))
		{
			Console.WriteLine("Cannot find directory: " + testsPath);
			throw new DirectoryNotFoundException(testsPath);
		}

		var fileNames = Directory.GetFiles(testsPath, "*.json", SearchOption.AllDirectories);
		var options = new EvaluationOptions
		{
			OutputFormat = OutputFormat.List
		};
		switch (draftFolder)
		{
			case "draft6":
				options.EvaluateAs = SpecVersion.Draft6;
				break;
			case "draft7":
				options.EvaluateAs = SpecVersion.Draft7;
				break;
			case "draft2019-09":
				options.EvaluateAs = SpecVersion.Draft201909;
				break;
			case "draft2020-12":
				options.EvaluateAs = SpecVersion.Draft202012;
				break;
			case "draft-next":
				// options.ValidateAs = SpecVersion.DraftNext;
				break;
		}

		foreach (var fileName in fileNames)
		{
			var shortFileName = Path.GetFileNameWithoutExtension(fileName);

			// adjust for format
			options.RequireFormatValidation = fileName.Contains("format/".AdjustForPlatform()) &&
											  // uri-template will throw an exception as it's explicitly unsupported
											  shortFileName != "uri-template";

			var contents = File.ReadAllText(fileName);
			var collections = JsonSerializer.Deserialize<List<TestCollection>>(contents, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			foreach (var collection in collections!)
			{
				collection.IsOptional = fileName.Contains("optional");
				collection.Options = EvaluationOptions.From(options);

				yield return collection;
			}
		}
	}

	private static void LoadRemoteSchemas()
	{
		// ReSharper disable once HeuristicUnreachableCode
		var remotesPath = Path.Combine(Directory.GetCurrentDirectory(), _benchmarkOffset, _remoteSchemasPath)
			.AdjustForPlatform();
		if (!Directory.Exists(remotesPath)) throw new Exception("Cannot find remotes folder");

		var fileNames = Directory.GetFiles(remotesPath, "*.json", SearchOption.AllDirectories);

		foreach (var fileName in fileNames)
		{
			var schema = JsonSchema.FromFile(fileName);
			var uri = new Uri(fileName.Replace(remotesPath, "http://localhost:1234").Replace('\\', '/'));
			SchemaRegistry.Global.Register(uri, schema);
		}
	}

	[GlobalSetup]
	public void BenchmarkSetup()
	{
		LoadRemoteSchemas();
		_allTests = GetAllTests().SelectMany(x => x.Tests.Where(t => InstanceIsDeserializable(t.Data)).Select(t => (x, t))).ToArray();
	}

	[Benchmark]
	[Arguments(1)]
	[Arguments(10)]
	public int RunSuite(int n)
	{
		int i = 0;
		var collections = _allTests;

		foreach (var (collection, test) in collections)
		{
			Benchmark(collection, test, n);
			i++;
		}

		return i;
	}

	private void Benchmark(TestCollection collection, TestCase test, int n)
	{
		if (!InstanceIsDeserializable(test.Data)) return;

		for (int i = 0; i < n; i++)
		{
			_ = collection.Schema.Evaluate(test.Data, collection.Options);
		}
	}

	private static bool InstanceIsDeserializable(in JsonNode? testData)
	{
		try
		{
			var value = (testData as JsonValue)?.GetValue<object>();
			if (value is null) return true;
			if (value is string) return false;
			if (value is JsonElement { ValueKind: JsonValueKind.Number } element)
				return element.TryGetDecimal(out _);
			if (value.GetType().IsNumber())
			{
				// some tests involve numbers larger than c# can handle.  fortunately, they're optional.
				return true;
			}

			return true;
		}
		catch
		{
			return false;
		}
	}
}