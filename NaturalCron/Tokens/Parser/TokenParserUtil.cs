using System.Text;
using NaturalCron.Rules;
using NaturalCron.Tokens.Parser.Dtos;
using NaturalCron.Tokens.Parser.ParseSpecStrategies;
using NaturalCron.Utils;

namespace NaturalCron.Tokens.Parser;

internal static class TokenParserUtil
{
    internal static IList<GroupedRuleSpec> GroupSpecs(IList<NaturalCronToken> tokens)
    {
        var groupSpecs = new HashSet<GroupedRuleSpec>();
        GroupedRuleSpec? currentGroupSpec = null;
        foreach (var token in tokens)
        {
            var firstTokenLower = token.Value.ToLower();
            if (firstTokenLower.StartsWith("@"))
            {
                firstTokenLower = firstTokenLower.Substring(1);
            }
            
            switch (firstTokenLower)
            {
                case "yearly":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Yearly
                    };

                    break;
                case "monthly":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Monthly
                    };
                    break;
                case "weekly":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Weekly
                    };
                    break;
                case "daily":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Daily
                    };
                    break;
                case "hourly":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Hourly
                    };
                    break;
                case "minutely":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Minutely
                    };
                    break;
                case "secondly":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Secondly
                    };
                    break;
                case "every":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.EveryX
                    };
                    break;
                case "between":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.Between
                    };
                    break;
                case "tz":
                case "timezone":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.TimeZone
                    };
                    break;
                case "at":
                case "on":
                case "in":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.AtInOn
                    };
                    break;
                case "upto":
                case "from":
                    currentGroupSpec = new GroupedRuleSpec()
                    {
                        Type = RuleSpecType.UptoOrFrom
                    };
                    break;
            }

            if (currentGroupSpec == null)
            {
                continue;
            }

            currentGroupSpec.Tokens.Add(token);
            groupSpecs.Add(currentGroupSpec);
        }

        return groupSpecs.ToList();
    }

    internal static List<NaturalCronToken> Tokenizer(string expression)
    {
        expression += "\0";
        StringBuilder accumulator = new StringBuilder();
        List<NaturalCronToken> tokens = new List<NaturalCronToken>();

        int currentLine = 1;
        int currentColumn = 1;
        char? holdingChar = null;
        for (int i = 0; i < expression.Length; i++)
        {
            var c = expression[i];

            bool shouldBreak = false;
            if (KeywordsConstants.Breaks.Contains(c))
            {
                shouldBreak = true;
            }
            else if (i > 0 && char.IsDigit(c) && !char.IsDigit(expression[i - 1]) )
            {
                holdingChar = c;
                shouldBreak = true;
            }
            else if (i > 0 && !char.IsDigit(c) && char.IsDigit(expression[i - 1]) && !IsNthWeekOrdinal(i, expression))
            {
                holdingChar = c;
                shouldBreak = true;
            }

            if (!shouldBreak)
            {
                accumulator.Append(c);
            }

            if (shouldBreak)
            {
                if (accumulator.Length > 0)
                {
                    NaturalCronTokenType tokenType = RecognizeTokenType(accumulator);
                    var token = new NaturalCronToken()
                    {
                        Type = tokenType,
                        Value = accumulator.ToString(),
                        IsValid = tokenType != NaturalCronTokenType.Unknown,
                        Error = tokenType == NaturalCronTokenType.Unknown ? "Invalid token" : string.Empty,
                        Line = currentLine,
                        StartCol = currentColumn - accumulator.Length,
                        EndCol = currentColumn
                    };
                    tokens.Add(token);
                    accumulator.Clear();
                }

                if (KeywordsConstants.Breaks.Contains(c))
                {
                    var breakType = RecognizeBreak(c);
                    // Ignores double spaces
                    if (breakType == NaturalCronTokenType.WhiteSpace && tokens.Count > 0 &&
                        tokens.Last().Type == NaturalCronTokenType.WhiteSpace)
                    {
                        continue;
                    }

                    var token = new NaturalCronToken()
                    {
                        Type = breakType,
                        Value = c.ToString(),
                        IsValid = breakType != NaturalCronTokenType.Unknown,
                        Error = breakType == NaturalCronTokenType.Unknown ? "Invalid token" : string.Empty,
                        Line = currentLine,
                        StartCol = currentColumn - accumulator.Length,
                        EndCol = currentColumn
                    };

                    if (breakType == NaturalCronTokenType.CloseBrackets)
                    {
                        var hasOpenBrackets = tokens.Any(x => x.Type == NaturalCronTokenType.OpenBrackets);
                        if (!hasOpenBrackets)
                        {
                            token.IsValid = false;
                            token.Error = "Missing open brackets";
                        }
                    }

                    tokens.Add(token);
                }
            }

            if (c == '\n')
            {
                currentLine++;
                currentColumn = 1;
            }
            else
            {
                currentColumn++;
            }

            if (holdingChar != null)
            {
                accumulator.Append(holdingChar.Value);
                holdingChar = null;
            }
        }

        return tokens;
    }
    
    private static bool IsNthWeekOrdinal(int i, string expression)
    {
        int start = i - 1;
        // Move start back to the beginning of the digit sequence
        while (start > 0 && char.IsDigit(expression[start - 1]))
            start--;

        int lookahead = i;
        while (lookahead < expression.Length && char.IsLetter(expression[lookahead]))
            lookahead++;

        var candidate = expression.Substring(start, lookahead - start);

        return KeywordsConstants.NthWeekDays.Any(kw => string.Equals(kw, candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static NaturalCronTokenType RecognizeBreak(char c)
    {
        switch (c)
        {
            case '[':
            {
                return NaturalCronTokenType.OpenBrackets;
            }
            case ']':
            {
                return NaturalCronTokenType.CloseBrackets;
            }
            case ' ':
            {
                return NaturalCronTokenType.WhiteSpace;
            }
            case '-':
            {
                return NaturalCronTokenType.DashOrMinus;
            }
            case '+':
                return NaturalCronTokenType.Plus;
            case ':':
            {
                return NaturalCronTokenType.Colon;
            }
            case ',':
            {
                return NaturalCronTokenType.Comma;
            }
            case '\0':
            {
                return NaturalCronTokenType.EndOfExpression;
            }
            default:
            {
                if (KeywordsConstants.IgnoredChars.Contains(c))
                {
                    return NaturalCronTokenType.IgnoredToken;
                }

                return NaturalCronTokenType.Unknown;
            }
        }
    }

    internal static (IList<NaturalCronRule> rules, IList<string> errors) ParseGroupedSpec(GroupedRuleSpec groupedRuleSpec)
    {
        var tokens = groupedRuleSpec.Tokens;
        if (tokens.First().Type != NaturalCronTokenType.RuleSpec)
        {
            throw new ArgumentException("First token must be Rule Spec");
        }

        var strategy = ParseSpecStrategyFactory.Create(groupedRuleSpec.Type);
        if (strategy == null)
        {
            throw new ArgumentException($"Strategy not found for type {groupedRuleSpec.Type}");
        }

        var result = strategy.Parse(groupedRuleSpec);
        return result;
    }

    internal static bool IsMonthWord(string value) =>
        KeywordsConstants.MonthWords.ToUpper().Any(x => value.ToUpper().ContainsWholeWord(x));

    internal static bool IsWeekdaySpecificWord(string value) =>
        KeywordsConstants.WeekdayWords.ToUpper().Any(x => value.ToUpper().ContainsWholeWord(x));

    internal static bool ContainsNthWeekdayWord(string value) =>
        KeywordsConstants.NthWeekDays.ToUpper().Any(x => value.ToUpper().ContainsWholeWord(x));

    internal static bool ContainsRelativeWeekdays(string value) =>
        KeywordsConstants.RelativeWeekdays.ToUpper().Any(x => value.ToUpper().ContainsWholeWord(x));

    internal static NaturalCronTimeUnitAndUnknown GetTimeUnit(string value)
    {
        if (KeywordsConstants.SecondsTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Second;
        }

        if (KeywordsConstants.MinutesTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Minute;
        }

        if (KeywordsConstants.HoursTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Hour;
        }

        if (KeywordsConstants.DaysTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Day;
        }

        if (KeywordsConstants.WeeksTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Week;
        }

        if (KeywordsConstants.MonthTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Month;
        }

        if (KeywordsConstants.YearsTimeUnitWords.ToUpper().Contains(value.ToUpper()))
        {
            return NaturalCronTimeUnitAndUnknown.Year;
        }

        return NaturalCronTimeUnitAndUnknown.Unknown;
    }

    internal static bool ContainsLastDayOfTheMonth(string value)
    {
        return KeywordsConstants.LastDayOfTheMonth.ToUpper().Any(x => value.ToUpper().Contains(x));
    }

    internal static bool ContainsFirstDayOfTheMonth(string value)
    {
        return KeywordsConstants.FirstDayOfTheMonth.ToUpper().Any(x => value.ToUpper().Contains(x));
    }

    internal static bool ContainsMax(string value)
    {
        return KeywordsConstants.Last.ToUpper().Any(x => value.ToUpper().Contains(x));
    }

    private static NaturalCronTokenType RecognizeTokenType(StringBuilder accumulator)
    {
        var tokenValue = accumulator.ToString().ToUpper();
        if (tokenValue.IsWholeNumber())
        {
            return NaturalCronTokenType.WholeNumber;
        }

        if (KeywordsConstants.RuleSpec.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.RuleSpec;
        }

        if (KeywordsConstants.Anchored.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.Anchored;
        }

        if (KeywordsConstants.TimeUnitWords.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.TimeUnit;
        }

        if (KeywordsConstants.MonthWords.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.MonthWord;
        }

        if (KeywordsConstants.WeekdayWords.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.WeekdayWord;
        }

        if (KeywordsConstants.IanaTimeZones.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.IanaTimeZoneId;
        }

        if (KeywordsConstants.LastDayOfTheMonth.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.LastDayOfTheMonth;
        }

        if (KeywordsConstants.FirstDayOfTheMonth.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.FirstDayOfTheMonth;
        }

        if (KeywordsConstants.RelativeWeekdays.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.RelativeWeekdays;
        }

        if (KeywordsConstants.NthWeekDays.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.NthWeekDays;
        }

        if (KeywordsConstants.Last.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.Last;
        }

        if (KeywordsConstants.First.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.First;
        }

        if (KeywordsConstants.PmOrAm.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.AmPm;
        }

        if (KeywordsConstants.And.ToUpper().Contains(tokenValue))
        {
            return NaturalCronTokenType.And;
        }

        return NaturalCronTokenType.Unknown;
    }

    internal static IList<NaturalCronToken> SkipTokens(IList<NaturalCronToken> tokens, params NaturalCronTokenType[] skipTypes)
    {
        return tokens.Where(x => !skipTypes.Contains(x.Type)).ToList();
    }

    internal static IList<NaturalCronToken> TrimTokens(IList<NaturalCronToken> tokens, params NaturalCronTokenType[] skipTypes)
    {
        // Remove leading whitespace tokens
        while (tokens.Count > 0 && skipTypes.Contains(tokens[0].Type))
        {
            tokens.RemoveAt(0);
        }

        // Remove trailing whitespace tokens
        while (tokens.Count > 0 && skipTypes.Contains(tokens[tokens.Count - 1].Type))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return tokens;
    }

    internal static IList<NaturalCronToken> TrimWhitespaceAndIgnoredTokens(IList<NaturalCronToken> tokens)
    {
        return TrimTokens(tokens, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.IgnoredToken);
    }

    internal static IList<NaturalCronToken> RemoveWhitespaceAndIgnoredTokens(IList<NaturalCronToken> tokens)
    {
        return SkipTokens(tokens, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.IgnoredToken);
    }

    internal static string JoinTokens(IList<NaturalCronToken> tokens, params NaturalCronTokenType[] skipTypes)
    {
        return JoinTokens(tokens, string.Empty, skipTypes);
    }

    internal static string JoinTokens(IList<NaturalCronToken> tokens, string separator, params NaturalCronTokenType[] skipTypes)
    {
        return string.Join(separator, tokens
            .Where(x => !skipTypes.Contains(x.Type))
            .Select(x => x.Value));
    }

    internal static IList<IList<NaturalCronToken>> SplitTokens(IList<NaturalCronToken> tokens, params NaturalCronTokenType[] skipTypes)
    {
        IList<IList<NaturalCronToken>> result = new List<IList<NaturalCronToken>>();
        IList<NaturalCronToken> currentList = new List<NaturalCronToken>();
        foreach (var token in tokens)
        {
            if (skipTypes.Contains(token.Type))
            {
                if (currentList.Count > 0)
                {
                    result.Add(currentList);
                    currentList = new List<NaturalCronToken>();
                }
            }
            else
            {
                currentList.Add(token);
            }
        }

        if (currentList.Count > 0)
        {
            result.Add(currentList);
        }

        return result;
    }

    // Parse tokens into time units.
    // e.g 10:30:10 -> [{10, hour}, {30, minute}, {10, second}]
    // e.g 10:30 -> [{10, hour}, {30, minute}]
    // e.g 2025-JAN-25 or 2025-01-25 -> [{2025, year}, {1, month}, {25, day}]
    // e.g JAN-25 or 01-25 -> [{1, month}, {25, day}]
    // e.g JAN -> [{1, month}]
    // e.g 10hr or hr 10 -> [{10, hour}]
    // e.g MON -> [{2, weekday}]
    internal static IList<TimeUnitAndValueDto> ParseTimeUnitValues(
        IList<NaturalCronToken> tokens,
        NaturalCronTimeUnitAndUnknown defaultTimeUnit = NaturalCronTimeUnitAndUnknown.Unknown)
    {
       var groupedTokens = GroupRelatedTokens(tokens);
        var result = new List<TimeUnitAndValueDto>();
        var ordinalPartnerIndex = -1;
        for (int i = 0; i < groupedTokens.Count; i++)
        {
            var tokenGroup = groupedTokens[i];
            
            if (tokenGroup.Any(x => x.Type == NaturalCronTokenType.Colon))
            {
                var colonBasedTimeUnitValues = ParseColonBasedTimeUnitValues(tokenGroup);
                result.AddRange(colonBasedTimeUnitValues);
                continue;
            }

            if (tokenGroup.Any(x => x.Type == NaturalCronTokenType.DashOrMinus) && !tokenGroup.Any(t => ContainsAnySpecialTimeToken(t)))
            {
                var dashBasedTimeUnitValues = ParseDashBasedTimeUnitValues(tokenGroup);
                result.AddRange(dashBasedTimeUnitValues);
                continue;
            }

            if (ordinalPartnerIndex >= 0 && i - ordinalPartnerIndex == 1 && defaultTimeUnit == NaturalCronTimeUnitAndUnknown.Unknown)
            {
                result.Add(ParseSingleTimeUnitValue(tokenGroup, NaturalCronTimeUnitAndUnknown.Month)); // the group after the ordinal partner default to month for "21st 10" -> 10 is the month.
                continue;
            }
            
            if (ordinalPartnerIndex >= 0 && i - ordinalPartnerIndex == 2 && defaultTimeUnit == NaturalCronTimeUnitAndUnknown.Unknown)
            {
                result.Add(ParseSingleTimeUnitValue(tokenGroup, NaturalCronTimeUnitAndUnknown.Year)); // the group after the ordinal partner default to month for "21st 10 2025" -> 2025 is the year.
                continue;
            }
            
            IList<NaturalCronToken>? nextTokenGroup = i < groupedTokens.Count - 1 ? groupedTokens[i + 1] : null;
            if (nextTokenGroup != null && 
                nextTokenGroup.Any(x => x.Type == NaturalCronTokenType.Colon) && 
                defaultTimeUnit == NaturalCronTimeUnitAndUnknown.Unknown)
            {
                result.Add(ParseSingleTimeUnitValue(tokenGroup, NaturalCronTimeUnitAndUnknown.Day));
                continue;
            }

            if (tokenGroup.Any(x => x.IsDayOrdinal()))
            {
                ordinalPartnerIndex = i;
            }
                
            result.Add(ParseSingleTimeUnitValue(tokenGroup, defaultTimeUnit));
        }
        
        return result;
    }

    /// <summary>
    /// Groups related tokens together while splitting by whitespace.
    /// Keeps tokens together when:
    /// - A TimeUnit token is followed by a value (e.g., "Day 1")
    /// - A number is followed by AM/PM (e.g., "10 am", "3 pm")
    /// </summary>
    private static IList<IList<NaturalCronToken>> GroupRelatedTokens(IList<NaturalCronToken> tokens)
    {
        var result = new List<IList<NaturalCronToken>>();
        var currentGroup = new List<NaturalCronToken>();
        
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            
            if (token.Type == NaturalCronTokenType.WhiteSpace || 
                token.Type == NaturalCronTokenType.Comma ||
                token.Type == NaturalCronTokenType.EndOfExpression)
            {
                NaturalCronToken? prevToken = null;
                for (var j = i - 1; j >= 0; j--)
                {
                    if (tokens[j].Type == NaturalCronTokenType.WhiteSpace)
                    {
                        continue;
                    }
                    
                    if (tokens[j].Type == NaturalCronTokenType.Comma)
                    {
                        continue;
                    }

                    if (tokens[j].Type == NaturalCronTokenType.EndOfExpression)
                    {
                        continue;
                    }
                    
                    prevToken = tokens[j];
                    break;
                }
               
                
                NaturalCronToken? nextToken = null;
                for (var j = i + 1; j < tokens.Count; j++)
                {
                    if (tokens[j].Type == NaturalCronTokenType.WhiteSpace)
                    {
                        continue;
                    }

                    if (tokens[j].Type == NaturalCronTokenType.Comma)
                    {
                        continue;
                    }
                    
                    if (tokens[j].Type == NaturalCronTokenType.EndOfExpression)
                    {
                        continue;
                    }
                    
                    nextToken = tokens[j];
                    break;
                }

                // Check if we should keep tokens together across this separator
                var shouldKeepTogether = false;
                
                if (prevToken?.Type == NaturalCronTokenType.TimeUnit && !prevToken.IsDayOrdinal() &&
                    (nextToken?.Type == NaturalCronTokenType.WholeNumber || nextToken?.IsDayOrdinal() == true))
                {
                    shouldKeepTogether = true;
                }
                else if (prevToken?.Type == NaturalCronTokenType.WholeNumber && nextToken?.Type == NaturalCronTokenType.AmPm)
                {
                    shouldKeepTogether = true;
                } 
                else if (prevToken?.Value.ToUpper().ContainsWholeWord("ClosestWeekdayTo".ToUpper()) == true)
                {
                    shouldKeepTogether = true;
                    currentGroup.Add(token);
                } else if (ContainsAnySpecialTimeToken(nextToken) && prevToken?.Type == NaturalCronTokenType.TimeUnit)
                {
                    shouldKeepTogether = true;
                } else if (ContainsAnySpecialTimeToken(prevToken) && nextToken?.Type == NaturalCronTokenType.TimeUnit)
                {
                    shouldKeepTogether = true;
                }  else if (ContainsAnySpecialTimeToken(prevToken) && nextToken?.Type == NaturalCronTokenType.Plus || nextToken?.Type == NaturalCronTokenType.DashOrMinus)
                {
                    shouldKeepTogether = true;
                } else if (nextToken?.Type == NaturalCronTokenType.WholeNumber && prevToken?.Type == NaturalCronTokenType.Plus || prevToken?.Type == NaturalCronTokenType.DashOrMinus)
                {
                    shouldKeepTogether = true;
                }
                
                if (!shouldKeepTogether && currentGroup.Count > 0)
                {
                    result.Add(currentGroup.ToList());
                    currentGroup.Clear();
                }
            }
            else if (token.Type != NaturalCronTokenType.WhiteSpace && 
                     token.Type != NaturalCronTokenType.Comma &&
                     token.Type != NaturalCronTokenType.EndOfExpression)
            {
                currentGroup.Add(token);
            }
        }
        
        // Add the last group if it has content
        if (currentGroup.Count > 0)
        {
            result.Add(currentGroup);
        }

        return result.Where(x => x.Count > 0).ToList();
    }

    private static bool ContainsAnySpecialTimeToken(NaturalCronToken? t)
    {
        if (t == null)
        {
            return false;
        }
        
        return t.Type == NaturalCronTokenType.Last ||
               t.Type == NaturalCronTokenType.First ||
               t.Type == NaturalCronTokenType.NthWeekDays ||
               t.Type == NaturalCronTokenType.RelativeWeekdays ||
               t.Type == NaturalCronTokenType.LastDayOfTheMonth ||
               t.Type == NaturalCronTokenType.FirstDayOfTheMonth;
    }

    private static bool HasWhitespaceBetweenTokens(
        IList<NaturalCronToken> tokens,
        NaturalCronTokenType separatorType)
    {
        var nonWhiteSpaceAndSeparator = new List<NaturalCronTokenType>
        {
            NaturalCronTokenType.WhiteSpace, separatorType
        };

        for (int i = 0; i < tokens.Count - 2; i++)
        {
            if (!nonWhiteSpaceAndSeparator.Contains(tokens[i].Type) &&
                tokens[i + 1].Type == NaturalCronTokenType.WhiteSpace &&
                tokens[i + 2].Type == separatorType)
            {
                return true;
            }

            if (tokens[i].Type == separatorType &&
                tokens[i + 1].Type == NaturalCronTokenType.WhiteSpace &&
                !nonWhiteSpaceAndSeparator.Contains(tokens[i + 2].Type))
            {
                return true;
            }
        }
        return false;
    }

    internal static TimeUnitAndValueDto ParseSingleTimeUnitValue(
        IList<NaturalCronToken> tokens,
        NaturalCronTimeUnitAndUnknown defaultTimeUnit = NaturalCronTimeUnitAndUnknown.Unknown)
    {
        var timeUnitToken = tokens.FirstOrDefault(x => x.Type == NaturalCronTokenType.TimeUnit);

        var tokensWithoutTimeUnit =
            tokens.Where(x => x.Type != NaturalCronTokenType.TimeUnit && 
                              x.Type != NaturalCronTokenType.EndOfExpression &&
                              x.Type != NaturalCronTokenType.AmPm).ToList();

        var invalidTokens = false;
        var dupesMathOperation = false;

        var value = JoinTokens(tokens,
            NaturalCronTokenType.EndOfExpression,
            NaturalCronTokenType.TimeUnit,
            NaturalCronTokenType.AmPm);

        if (TokenParserUtil.IsWeekdaySpecificWord(value))
        {
            return new()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Week, Value = value.Trim(), IsValid = true, Error = string.Empty
            };
        }

        if (TokenParserUtil.IsMonthWord(value))
        {
            return
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Month, Value = value.Trim(), IsValid = true, Error = string.Empty
                };
        }

        if (ContainsLastDayOfTheMonth(value) ||
            ContainsNthWeekdayWord(value) ||
            ContainsRelativeWeekdays(value) ||
            ContainsFirstDayOfTheMonth(value))
        {
            invalidTokens = tokensWithoutTimeUnit.Any(x =>
                x.Type != NaturalCronTokenType.DashOrMinus &&
                x.Type != NaturalCronTokenType.Plus &&
                x.Type != NaturalCronTokenType.WhiteSpace &&
                x.Type != NaturalCronTokenType.WholeNumber &&
                x.Type != NaturalCronTokenType.RelativeWeekdays &&
                x.Type != NaturalCronTokenType.LastDayOfTheMonth &&
                x.Type != NaturalCronTokenType.FirstDayOfTheMonth &&
                x.Type != NaturalCronTokenType.NthWeekDays &&
                x.Type != NaturalCronTokenType.First);

            dupesMathOperation = tokensWithoutTimeUnit.Count(x =>
                x.Type == NaturalCronTokenType.DashOrMinus &&
                x.Type == NaturalCronTokenType.Plus) > 1;

            if (invalidTokens || dupesMathOperation)
            {
                return new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown, Value = value.Trim(), IsValid = false, Error = "invalid expression"
                };
            }

            return new()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Day, Value = value.Trim(), IsValid = true, Error = string.Empty
            };
            
        }
        
        NaturalCronTimeUnitAndUnknown timeUnit = defaultTimeUnit;
        if (timeUnitToken != null)
        {
            timeUnit = GetTimeUnit(timeUnitToken.Value);
        }
       
        var pmOrAmToken = tokens.FirstOrDefault(x => x.Type == NaturalCronTokenType.AmPm);
        if (pmOrAmToken != null)
        {
            timeUnit = NaturalCronTimeUnitAndUnknown.Hour;
            value = ParseHourValue(value.Trim(), pmOrAmToken);
        }
        
        if (timeUnit == NaturalCronTimeUnitAndUnknown.Unknown)
        {
            return new()
            {
                TimeUnit = timeUnit,
                Value = value.Trim(),
                IsValid = false,
                Error = "invalid time unit",
            };
        }

        invalidTokens = tokensWithoutTimeUnit.Any(x =>
            x.Type != NaturalCronTokenType.WhiteSpace &&
            x.Type != NaturalCronTokenType.WholeNumber &&
            x.Type != NaturalCronTokenType.Last &&
            x.Type != NaturalCronTokenType.First &&
            x.Type != NaturalCronTokenType.LastDayOfTheMonth &&
            x.Type != NaturalCronTokenType.FirstDayOfTheMonth &&
            x.Type != NaturalCronTokenType.DashOrMinus &&
            x.Type != NaturalCronTokenType.Plus);

        dupesMathOperation = tokensWithoutTimeUnit.Count(x =>
            x.Type == NaturalCronTokenType.DashOrMinus ||
            x.Type == NaturalCronTokenType.Plus) > 1;

        if (invalidTokens || dupesMathOperation)
        {
            return new()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown, 
                Value = value.Trim(), 
                IsValid = false, 
                Error = "invalid expression",
            };
        }
        
        return new()
        {
            TimeUnit = timeUnit,
            Value = value.Trim(),
            IsValid = true,
            Error = string.Empty,
        };
    }

    internal static IList<TimeUnitAndValueDto> ParseDashBasedTimeUnitValues(IList<NaturalCronToken> tokens)
    {
        var splitTokens = SplitTokens(tokens, NaturalCronTokenType.DashOrMinus);
        string year = string.Empty;
        string month = string.Empty;
        string day = string.Empty;

        var dashCount = tokens.Count(x => x.Type == NaturalCronTokenType.DashOrMinus);
        if (dashCount > 2)
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Year,
                    Value = year.Trim(),
                    IsValid = true,
                    Error = $"Invalid format yyyy-MM or yyyy-MM-dd or MM-dd"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.SecondsTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use sec time units with dashes"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.MinutesTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use min time units with dashes"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.HoursTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use hr time units with dashes"
                }
            };
        }

        var tokens1 = splitTokens[0].Where(x => x.Type == NaturalCronTokenType.WholeNumber ||
                                                x.Type == NaturalCronTokenType.Last ||
                                                x.Type == NaturalCronTokenType.MonthWord).ToList();

        var tokens2 = splitTokens[1].Where(x => x.Type == NaturalCronTokenType.WholeNumber ||
                                                x.Type == NaturalCronTokenType.MonthWord ||
                                                x.Type == NaturalCronTokenType.Last).ToList();
        var tokens3 = splitTokens.Count > 2
            ? splitTokens[2].Where(x => x.Type == NaturalCronTokenType.WholeNumber ||
                                        x.Type == NaturalCronTokenType.LastDayOfTheMonth ||
                                        x.Type == NaturalCronTokenType.Last).ToList()
            : new List<NaturalCronToken>();
        if (tokens3.Count > 0)
        {
            year = JoinTokens(tokens1, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.EndOfExpression);
            month = JoinTokens(tokens2, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.EndOfExpression);
            day = JoinTokens(tokens3, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.EndOfExpression);
        }
        else
        {
            var yearOrMonth = JoinTokens(tokens1, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.EndOfExpression);
            var dayOrMonth = JoinTokens(tokens2, NaturalCronTokenType.WhiteSpace, NaturalCronTokenType.EndOfExpression);
            if (yearOrMonth.Length == 4 && yearOrMonth.IsWholeNumber())
            {
                year = yearOrMonth;
                month = dayOrMonth;
            }
            else
            {
                month = yearOrMonth;
                day = dayOrMonth;
            }
        }

        if (!string.IsNullOrEmpty(year) && (year.Length != 4 || !year.IsWholeNumber()))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    IsValid = false,
                    Error = $"Invalid format yyyy-MM or yyyy-MM-dd or MM-dd"
                }
            };
        }

        if (!string.IsNullOrEmpty(month) && month.Length != 2 &&
            !TokenParserUtil.IsMonthWord(month) &&
            !TokenParserUtil.ContainsMax(day))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    IsValid = false,
                    Error = $"Invalid format yyyy-MM or yyyy-MM-dd or MM-dd"
                }
            };
        }

        if (!string.IsNullOrEmpty(day) &&
            day.Length != 2 &&
            !TokenParserUtil.ContainsLastDayOfTheMonth(day) &&
            !TokenParserUtil.ContainsMax(day))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    IsValid = false,
                    Error = $"Invalid format yyyy-MM or yyyy-MM-dd or MM-dd"
                }
            };
        }

        var result = new List<TimeUnitAndValueDto>();
        if (!string.IsNullOrEmpty(year))
        {
            result.Add(new TimeUnitAndValueDto()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Year, Value = year.Trim(), IsValid = true
            });
        }

        if (!string.IsNullOrEmpty(month))
        {
            result.Add(new TimeUnitAndValueDto()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Month, Value = month.Trim(), IsValid = true
            });
        }

        if (!string.IsNullOrEmpty(day))
        {
            result.Add(new TimeUnitAndValueDto()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Day, Value = day.Trim(), IsValid = true
            });
        }

        return result;
    }

    internal static IList<TimeUnitAndValueDto> ParseColonBasedTimeUnitValues(IList<NaturalCronToken> tokens)
    {
        var countColons = tokens.Count(x => x.Type == NaturalCronTokenType.Colon);
        if (countColons > 2)
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "Invalid format it should be HH:MM:SS or HH:MM"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.DaysTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use days time units with colon"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.WeeksTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use week time units with colon"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.MonthTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use month time units with colon"
                }
            };
        }

        if (tokens.Where(x => x.Type == NaturalCronTokenType.TimeUnit)
            .Any(x => KeywordsConstants.YearsTimeUnitWords
                .ToUpper()
                .Contains(x.Value.ToUpper())))
        {
            return new List<TimeUnitAndValueDto>()
            {
                new()
                {
                    TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                    Value = string.Empty,
                    IsValid = false,
                    Error = "can not use year time units with colon"
                }
            };
        }


        var splitTokens = SplitTokens(tokens, NaturalCronTokenType.Colon);
        var hourTokens = splitTokens[0].Where(x => x.Type == NaturalCronTokenType.WholeNumber ||
                                                   x.Type == NaturalCronTokenType.Last).ToList();

        var hourValue = JoinTokens(hourTokens, NaturalCronTokenType.EndOfExpression);

        var hasAmOrPm = tokens.Any(x => x.Type == NaturalCronTokenType.AmPm);
        if (hasAmOrPm && hourValue.IsWholeNumber())
        {
            var amOrPmTokens = tokens.Where(x => x.Type == NaturalCronTokenType.AmPm).ToList();
            var pmOrAm = amOrPmTokens.LastOrDefault();
            if (amOrPmTokens.Count > 1 || pmOrAm?.Type != NaturalCronTokenType.AmPm)
            {
                return new List<TimeUnitAndValueDto>()
                {
                    new()
                    {
                        TimeUnit = NaturalCronTimeUnitAndUnknown.Unknown,
                        Value = string.Empty,
                        IsValid = false,
                        Error = "Invalid format it should be HH:MM:SS AM/PM or HH:MM AM/PM"
                    }
                };
            }

            hourValue = ParseHourValue(hourValue, pmOrAm);
        }

        var minuteTokens = splitTokens[1]
            .Where(x => x.Type == NaturalCronTokenType.WholeNumber ||
                        x.Type == NaturalCronTokenType.Last)
            .ToList();
        var secondTokens = splitTokens.Count > 2 ? splitTokens[2] : new List<NaturalCronToken>();
        secondTokens = secondTokens.Where(x => x.Type == NaturalCronTokenType.WholeNumber ||
                                               x.Type == NaturalCronTokenType.Last).ToList();

        var result = new List<TimeUnitAndValueDto>()
        {
            new()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Hour, Value = hourValue, IsValid = true
            },
            new()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Minute,
                Value = JoinTokens(minuteTokens, NaturalCronTokenType.EndOfExpression),
                IsValid = true
            },
        };

        if (secondTokens.Count > 0)
        {
            result.Add(new TimeUnitAndValueDto()
            {
                TimeUnit = NaturalCronTimeUnitAndUnknown.Second,
                Value = JoinTokens(secondTokens, NaturalCronTokenType.EndOfExpression),
                IsValid = true
            });
        }

        return result;
    }

    private static string ParseHourValue(string hourValue, NaturalCronToken pmOrAm)
    {
        var hourInt = int.Parse(hourValue);
        if (pmOrAm.Value.ToUpper() == "PM" && hourInt != 12)
        {
            hourInt += 12; // Convert PM hours to 24-hour format
        }
        else if (pmOrAm.Value.ToUpper() == "AM" && hourInt == 12)
        {
            hourInt = 0; // Convert 12 AM to 0 hours
        }
        
        return hourInt.ToString();
    }

    internal static string? GetAnchoredValue(IList<NaturalCronToken> tokens)
    {
        var anchoredIndex = -1;
        if (tokens.Any(x => x.Type == NaturalCronTokenType.Anchored))
        {
            anchoredIndex = tokens.IndexOf(tokens.First(x => x.Type == NaturalCronTokenType.Anchored));
        }

        string? anchoredValue = null;
        if (anchoredIndex != -1)
        {
            anchoredValue = JoinTokens(tokens.Skip(anchoredIndex + 1).ToList(), skipTypes:
            [
                NaturalCronTokenType.WhiteSpace,
                NaturalCronTokenType.IgnoredToken,
                NaturalCronTokenType.EndOfExpression,
                NaturalCronTokenType.Comma,
                NaturalCronTokenType.TimeUnit,
            ]);
        }
        return anchoredValue;
    }
}