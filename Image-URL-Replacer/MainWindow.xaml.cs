using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageUrlReplacer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private const string _APP_NAME = "Image URL Replacer";
		
		
		private string _text = "";
		private string _url = "";
		private int _index = 0;
		private int _lenght = 0;
		private readonly History[] _history = new History[10];
		private int _historyIndex = 0;
		private bool _isSaved = true;
		private string _fileName = "";

		class History
		{
			public History(int index, string text, string oldText)
			{
				Index = index;
				Text = text;
				OldText = oldText;
			}
			
			public int Index { get; set; }
			public string Text { get; set; }
			public string OldText { get; set; }
		}

		public MainWindow()
		{
			InitializeComponent();
			Form.Title = _APP_NAME;
		}

		private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
		private void CommandSave_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = _text != "";

		private void CommandCopy_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = TextField.Text != "";

		private void CommandUndo_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = _historyIndex < _history.Length && _history[_historyIndex] != null;

		private void CommandRedo_CanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = _historyIndex != 0;
		
		private void CommandOpen_Executed(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "All files (*.*)|*.*|Text files (*.txt)|*.txt|JSON|*.json|HTML|*.html|XML|*.xml|XAML|*.xaml",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
			};

			if (dialog.ShowDialog() == true)
			{
				_fileName = dialog.FileName;
				Form.Title = $"{_fileName} - {_APP_NAME}";
				_isSaved = true;
				_text = File.ReadAllText(dialog.FileName);
				_lenght = _text.Length;
				TextField.Text = "";
				Image.Source = null;
				FindUrl();
			}
		}

		private void CommandNext_Executed(object sender, RoutedEventArgs e)
		{
			CheckForChange();
			FindUrl();
		}
		private void CommandPrevious_Executed(object sender, RoutedEventArgs e)
		{
			CheckForChange();
			FindUrl(false);
		}

		private void CheckForChange()
		{
			if (TextField.Text == _url)
			{
				return;
			}

			string url = TextField.Text.Trim();
			if (url != _url && IsImageUrl(url))
			{
				ChangeUrl();
			}
		}
		
		private bool IsImageUrl(string text)
		{
			if (text.EndsWith(".png") || text.EndsWith(".jpg") || text.EndsWith(".gif") || text.EndsWith(".jpeg"))
			{
				return true;
			}
			text = text.Split('?')[0];
			return text.EndsWith(".png") || text.EndsWith(".jpg") || text.EndsWith(".gif") || text.EndsWith(".jpeg");
		}
		private void FindUrl(bool direction = true)
		{
			if (_text == "")
				return;
			
			_index += direction ? _url.Length : -1;

			if (_index >= _lenght)
			{
				_index = 0;
			} 
			else if (_index < 0)
			{
				_index = _lenght - 10;
			}

			if (direction)
			{
				int startIndex = _index;
				while (!GetLink())
				{
					_index++;
					if (_index >= _lenght)
					{
						_index = 0;
					} 
					if (_index == startIndex)
					{
						return;
					}
				}
			}
			else
			{
				int startIndex = _index;
				while (!GetLink())
				{
					_index--;
					if (_index < 0)
					{
						_index = _lenght - 1;
					}
					if (_index == startIndex)
					{
						return;
					}
				}
			}

			bool GetLink()
			{
				if (_text[_index] != 'h' || _text[_index + 1] != 't' || _text[_index + 2] != 't' || _text[_index + 3] != 'p')
				{
					return false;
				}

				int newIndex = _index + 4;
				char c = _text[_index-1];
				if (_text[_index - 2] == '\\')
				{
					while (newIndex < _lenght && _text[newIndex] != ' ' && _text[newIndex] != '\n' && _text[newIndex] != '\r' && _text[newIndex] != '\t' && (_text[newIndex] != '\\' || _text[newIndex+1] != c))
					{
						newIndex++;
					}
				}
				else
				{
					while (newIndex < _lenght && _text[newIndex] != ' ' && _text[newIndex] != '\n' && _text[newIndex] != '\r' && _text[newIndex] != '\t' && _text[newIndex] != c)
					{
						newIndex++;
					}
				}
				string url = _text.Substring(_index, newIndex - _index);
				if (IsImageUrl(url))
				{
					_url = url;
					TextField.Text = url;
					Image.Source = new BitmapImage(new Uri(url));
					return true;
				}
				return false;
			}
		}

		private void ChangeUrl()
		{
			if (_isSaved)
			{
				Form.Title = $"*{_fileName} - {_APP_NAME}";
				_isSaved = false;
			}
			
			string url = TextField.Text.Trim();
			ShiftHistory(true);
			AddHistory(new History(_index, url, _url));
			string newText = _text.Substring(0, _index) + url + _text.Substring(_index + _url.Length);
			_url = url;
			_text = newText;
		}

		private void CommandUndo_Executed(object sender, RoutedEventArgs e) => UnRedo(true);

		private void CommandRedo_Executed(object sender, RoutedEventArgs e) => UnRedo(false);

		private void UnRedo(bool direction)
		{
			if (direction)
			{
				if (_historyIndex >= _history.Length)
				{
					_historyIndex = _history.Length;
					return;
				}
			}
			else
			if (--_historyIndex < 0)
			{
				_historyIndex = 0;
				return;
			}
			
			History h = _history[_historyIndex];
			if (direction)
			{
				_text = _text.Substring(0, h.Index) + h.OldText + _text.Substring(h.Index + h.OldText.Length);
				_url = h.OldText;
			}
			else
			{
				_text = _text.Substring(0, h.Index) + h.Text + _text.Substring(h.Index + h.Text.Length);
				_url = h.Text;
			}
			
			_index = h.Index;
			
			TextField.Text = _url;
			Image.Source = new BitmapImage(new Uri(_url));

			if (direction)
			{
				_historyIndex++;
			}
		}

		private void ShiftHistory(bool direction)
		{
			if (direction)
			{
				for (int i = 0; i < _history.Length - 1; i++)
				{
					_history[i + 1] = _history[i];
				}

				_history[0] = null;
			}
			else
			{
				for (int i = 0; i < _history.Length - 1; i++)
				{
					_history[i] = _history[i + 1];
				}

				_history[_history.Length - 1] = null;
			}
		}

		private void AddHistory(History history)
		{
			while (_historyIndex > 1)
			{
				ShiftHistory(false);
			}

			if (_historyIndex == 0)
			{
				ShiftHistory(true);
			}

			_history[0] = history;
		}

		private void CommandCopy_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (TextField.IsSelectionActive) return;
			Clipboard.SetText(TextField.Text);
		}

		private void CommandPaste_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (TextField.IsSelectionActive) return;
			string clip = Clipboard.GetText().Trim();
			if (clip == _url && clip == TextField.Text || !IsImageUrl(clip))
			{
				return;
			}
			TextField.Text = clip;
			ChangeUrl();
		}
		private void CommandCut_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (TextField.IsSelectionActive) return;
			Clipboard.SetText(TextField.Text);
			TextField.Text = "";
		}

		private void CommandSaveAs_Executed(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog()
			{
				Filter = "All files (*.*)|*.*",
				//InitialDirectory = _fileName.Substring(_fileName.LastIndexOf('/')),
				DefaultExt = _fileName.Split('.').Last(),
				FileName = _fileName,
				OverwritePrompt = true,
			};

			if (dialog.ShowDialog() == true)
			{
				using (StreamWriter writer = new StreamWriter(dialog.OpenFile()))  
				{
					writer.Write(_text);
				}
				
				_fileName = dialog.FileName;
				Form.Title = $"{_fileName} - {_APP_NAME}";
				_isSaved = true;
				_text = File.ReadAllText(dialog.FileName);
				_lenght = _text.Length;
				TextField.Text = "";
				Image.Source = null;
				FindUrl();
			}
		}

		private void CommandSave_Executed(object sender, RoutedEventArgs e)
		{
			CheckForChange();
			if (_isSaved)
			{
				return;
			}
			using (var writer = new StreamWriter(_fileName, false))  
			{
				writer.Write(_text);
			}

			_isSaved = true;
			Form.Title = $"{_fileName} - {_APP_NAME}";
		}

		private void MainWindow_OnDrop(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				return;
			}

			// Note that you can have more than one file.
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

			if (files == null)
			{
				return;
			}

			_text = File.ReadAllText(files[0]);
			_fileName = files[0];
			Form.Title = $"{_fileName} - {_APP_NAME}";
			_isSaved = true;
			_lenght = _text.Length;
			TextField.Text = "";
			Image.Source = null;
			FindUrl();
		}
	}
}