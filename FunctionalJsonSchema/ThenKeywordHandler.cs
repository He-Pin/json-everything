﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace FunctionalJsonSchema;

public class ThenKeywordHandler : IKeywordHandler
{
	public static ThenKeywordHandler Instance { get; } = new();

	public string Name => "then";
	public string[]? Dependencies { get; } = ["if"];

	private ThenKeywordHandler() { }

	public KeywordEvaluation Handle(JsonNode? keywordValue, EvaluationContext context, IReadOnlyCollection<KeywordEvaluation> evaluations)
	{
		var ifEvaluation = evaluations.FirstOrDefault(x => x.Key == "if");
		if (ifEvaluation is null || !ifEvaluation.Children[0].Valid) return KeywordEvaluation.Skip;

		var localContext = context;
		localContext.EvaluationPath = localContext.EvaluationPath.Combine(Name);
		localContext.SchemaLocation = localContext.SchemaLocation.Combine(Name);

		var result = localContext.Evaluate(keywordValue);

		return new KeywordEvaluation
		{
			Valid = result.Valid,
			Children = [result]
		};
	}

	JsonNode?[] IKeywordHandler.GetSubschemas(JsonNode? keywordValue) => [keywordValue];
}