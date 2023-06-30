using FluentValidation;

class FeedCreateValidator : AbstractValidator<FeedCreateDocument> {
  public FeedCreateValidator() {
    RuleFor(x => x.Name).MinimumLength(3).MaximumLength(15);
    RuleFor(x => x.Template).NotNull();
  }
}