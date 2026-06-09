namespace NetworkService.Model
{
    public class SavedSearch
    {
        public string SearchBy { get; set; }  // "Name" or "Type"
        public string SearchText { get; set; }

        public override string ToString() => $"{SearchBy}: \"{SearchText}\"";
    }
}