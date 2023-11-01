#region Copyright (c) Pixeval/Pixeval
// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2022 Pixeval/IIllustrationVisualizer.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System.Collections.ObjectModel;
using Pixeval.UserControls.IllustrationView;

namespace Pixeval.Pages.IllustrationViewer;

public interface IIllustrationVisualizer
{
    /// <summary>
    /// Dispose current visualizing illustrations, behaves like Clear
    /// </summary>
    void DisposeCurrent();

    ObservableCollection<IllustrationViewModel> Illustrations { get; set; }

    /// <summary>
    /// Add a illustration view model to visualizer
    /// </summary>
    /// <param name="viewModel">The view model, usually fetched from FetchEngine</param>
    void AddIllustrationViewModel(IllustrationViewModel viewModel);
}
