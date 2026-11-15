namespace TickTickModels;

public class FocusSyncResultBean : ErrorModel
{
	public FocusSyncStatusModel current { get; set; }

	public long point { get; set; }

	public FocusSyncStatusModel[] updates { get; set; }
}
