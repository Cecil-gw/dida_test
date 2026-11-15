namespace KotlinModels;

public class DueDataResult
{
	public DueDataModel revise;

	public DueDataModel origin;

	public BatchEditModel batchEditModel = new BatchEditModel();

	public int isReminderChanged;

	public int onlyDateChanged;
}
