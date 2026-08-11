using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Represents a single node (folder or configuration file) in the hierarchical device
    /// configuration tree built from the config directory, preserving nesting for cascading
    /// menu display.
    /// </summary>
    public sealed class DeviceConfigTreeNode : BaseViewModel
        {
        private bool _isSelected;

        /// <summary>
        /// Gets the display text for the node: the folder name, or the config file name without extension.
        /// </summary>
        public required string DisplayName { get; init; }

        /// <summary>
        /// Gets the path relative to the config directory (without extension), used as the unique
        /// selection identifier and stored on <see cref="DeviceUnderTest.DeviceConfiguration"/>.
        /// Null for folder nodes.
        /// </summary>
        public string? RelativePath { get; init; }

        /// <summary>
        /// Gets whether this node represents a folder rather than a configuration file.
        /// </summary>
        public bool IsFolder { get; init; }

        /// <summary>
        /// Gets the command executed when this node's menu entry is clicked. Set only on configuration
        /// file nodes; null for folder nodes, which only expand their submenu.
        /// </summary>
        public ICommand? SelectCommand { get; init; }

        /// <summary>
        /// Gets or sets whether this configuration file node is the currently selected device configuration,
        /// driving the checkmark shown next to it in the cascading menu.
        /// </summary>
        public bool IsSelected
            {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
            }

        /// <summary>
        /// Gets the child nodes (subfolders followed by configuration files). Empty for configuration file nodes.
        /// </summary>
        public ObservableCollection<DeviceConfigTreeNode> Children { get; } = [];
        }
    }
