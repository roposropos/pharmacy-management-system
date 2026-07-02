using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public abstract partial class EditableViewModelBase : ViewModelBase, INotifyDataErrorInfo, IHasId
{
	protected readonly Dictionary<string, List<string>> Errors = new();
	[ObservableProperty] private bool _isModified;
	public virtual int Id { get; set; }
	public bool HasErrors => Errors.Any();
	public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

	public IEnumerable GetErrors(string? propertyName)
	{
		if (string.IsNullOrEmpty(propertyName) || !Errors.TryGetValue(propertyName, out var errors))
			return Enumerable.Empty<string>();
		return errors;
	}

	protected void OnErrorsChanged(string propertyName)
	{
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		OnPropertyChanged(nameof(HasErrors));
	}
}