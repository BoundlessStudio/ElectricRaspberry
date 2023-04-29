using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using System.Globalization;

class ConvertSkill 
{
    [SKFunction("convert a date to a number of seconds")]
    [SKFunctionName("ConvertDateToSeconds")]
    [SKFunctionInput(Description = "The a date to convert")]
    [SKFunctionContextParameter(Name = "Timezone", Description = "controls which timezone the date is Assume to be in: 'utc' or 'local'", DefaultValue = "local")]
    public Task<string> ConvertDateToNumberAsync(string initialValueText, SKContext context)
    {
      var style = DateTimeStyles.None;
      string contextAmount = context["Timezone"];
      switch (contextAmount)
      {
        case "utc":
          style = DateTimeStyles.AssumeUniversal;
          break;
        case "local":
          style = DateTimeStyles.AssumeLocal;
          break;
        default:
          return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(context), contextAmount, "Context timezone provided is not a valid type"));
      }
      
      if (!DateTimeOffset.TryParse(initialValueText, CultureInfo.InvariantCulture, style, out var initialValue))
        return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(initialValueText), initialValueText, "Initial value provided is not in date format"));

      var result = initialValue.ToUnixTimeSeconds();
      return Task.FromResult(result.ToString(CultureInfo.InvariantCulture));
    }

    [SKFunction("Convert a number of seconds to a Date")]
    [SKFunctionName("ConvertSecondsToDate")]
    [SKFunctionInput(Description = "The number seconds to convert")]
    [SKFunctionContextParameter(Name = "Timezone", Description = "controls which timezone the date is returned in: 'utc' or 'local'", DefaultValue = "local")]
    public Task<string> ConvertNumberToDateAsync(string initialValueText, SKContext context)
    {
      if (!long.TryParse(initialValueText, CultureInfo.InvariantCulture, out var initialValue))
      {
        return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(initialValueText), initialValueText, "Initial value provided is not in numeric format"));
      }

      var result = DateTimeOffset.FromUnixTimeSeconds(initialValue);

      string contextAmount = context["Timezone"];
      switch (contextAmount)
      {
        case "utc":
          return Task.FromResult(result.ToString("f", CultureInfo.CurrentCulture));
        case "local":
          return Task.FromResult(result.LocalDateTime.ToString("f", CultureInfo.CurrentCulture));
        default:
          return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(context), contextAmount, "Context timezone provided is not a valid type"));
      }
    }

    [SKFunction("Converts a timeSpan to a number of seconds")]
    [SKFunctionName("ConvertTimeSpanToSeconds")]
    [SKFunctionInput(Description = "The TimeSpan to convert")]
    public Task<string> ConvertTimeSpanToNumberAsync(string initialValueText, SKContext context)
    {
      if (!TimeSpan.TryParse(initialValueText, CultureInfo.InvariantCulture, out var initialValue))
      {
        return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(initialValueText), initialValueText, "Initial value provided is not in timeSpan format"));
      }

      return Task.FromResult(initialValue.TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [SKFunction("Converts a Number of Days, Hours, Minutes, or Seconds to a TimeSpan")]
    [SKFunctionName("ConvertNumberToTimeSpan")]
    [SKFunctionInput(Description = "The Number to convert")]
    [SKFunctionContextParameter(Name = "Component", Description = "Create timeSpan from total number of Days, Hours, Minutes, or Seconds.", DefaultValue = "Seconds")]
    public Task<string> ConvertNumberToTimeSpanAsync(string initialValueText, SKContext context)
    {
      if (!int.TryParse(initialValueText, CultureInfo.InvariantCulture, out var initialValue))
      {
        return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(initialValueText), initialValueText, "Initial value provided is not in timeSpan format"));
      }

      string contextAmount = context["Component"];
      switch (contextAmount)
      {
        case "Days":
          return Task.FromResult(TimeSpan.FromDays(initialValue).ToString());
        case "Hours":
          return Task.FromResult(TimeSpan.FromHours(initialValue).ToString());
        case "Minutes":
          return Task.FromResult(TimeSpan.FromMinutes(initialValue).ToString());
        case "Seconds":
          return Task.FromResult(TimeSpan.FromSeconds(initialValue).ToString());
        default:
          return Task.FromException<string>(new ArgumentOutOfRangeException(nameof(context), contextAmount, "Context component provided is not a valid type"));
      }
    }
}