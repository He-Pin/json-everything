﻿using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FunctionalJsonSchema;

public class DefaultKeywordHandler : IKeywordHandler
{
	public static DefaultKeywordHandler Instance { get; } = new();

	public string Name => "default";
	public string[]? Dependencies { get; }

	private DefaultKeywordHandler() { }

	public KeywordEvaluation Handle(JsonNode? keywordValue, EvaluationContext context, IReadOnlyCollection<KeywordEvaluation> siblingEvaluations)
	{
		return KeywordEvaluation.Annotate;
	}

	JsonNode?[] IKeywordHandler.GetSubschemas(JsonNode? keywordValue) => [];
}