namespace Apteka.ViewModels.Grid;

public interface IFilterable
{
	bool IsMatch(string searchText);
}