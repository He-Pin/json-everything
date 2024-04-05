﻿using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FunctionalJsonSchema;

public class IfKeywordHandler : IKeywordHandler
{
	public static IfKeywordHandler Instance { get; } = new();

	public string Name => "if";
	public string[]? Dependencies { get; }

	private IfKeywordHandler() { }

	public KeywordEvaluation Handle(JsonNode? keywordValue, EvaluationContext context, IReadOnlyCollection<KeywordEvaluation> evaluations)
	{
		var localContext = context;
		localContext.EvaluationPath = localContext.EvaluationPath.Combine(Name);
		localContext.SchemaLocation = localContext.SchemaLocation.Combine(Name);

		var result = localContext.Evaluate(keywordValue);

		return new KeywordEvaluation
		{
			Valid = true,
			Children = [result]
		};
	}

	JsonNode?[] IKeywordHandler.GetSubschemas(JsonNode? keywordValue) => [keywordValue];
}