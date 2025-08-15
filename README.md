# ItemBag Editor

A Windows desktop application for editing ItemBag configuration files used in MU Online private servers. Built with WPF and .NET 6, this tool provides an intuitive interface for managing item drop configurations, class restrictions, and bag settings.

## 🚀 Features

- **Bag Configuration Management**: Edit all ItemBag attributes including drop rates, set item settings, party drop configurations, and more
- **Dynamic UI Generation**: Automatically creates configuration tabs based on your ItemBag XML structure
- **Auto-Save Functionality**: Configuration changes are automatically saved when switching between tabs
- **Class Restriction Editor**: Manage which character classes can access specific item bags
- **Item Drop Management**: Configure individual item drop rates, categories, and levels
- **Registry-Based Settings**: Application settings are stored in the Windows Registry for better security
- **Modern WPF Interface**: Clean, responsive user interface built with Windows Presentation Foundation

## 🛠️ System Requirements

- **Operating System**: Windows 10/11 (Windows 7 compatible with build:win7 target)
- **.NET Runtime**: .NET 6.0 Desktop Runtime or later
- **Memory**: 512 MB RAM minimum
- **Storage**: 100 MB available disk space

## 📦 Installation

### Option 1: Download Pre-built Executable
1. Download the latest release from the [Releases](https://github.com/inguz-BA/itembageditor/releases) page
2. Extract the ZIP file to your desired location
3. Run `ItemBagEditor.exe`

### Option 2: Build from Source
1. Clone the repository:
   ```bash
   git clone https://github.com/inguz-BA/itembageditor.git
   cd itembageditor
   ```
2. Ensure you have .NET 6.0 SDK installed
3. Build the application:
   ```bash
   npm run tauri build
   ```
   Or for Windows 7 compatibility:
   ```bash
   npm run build:win7
   ```

## 🎯 Usage

### Opening ItemBag Files
1. Launch the application
2. Use **File → Open** or press **Ctrl+O**
3. Navigate to your ItemBag XML file and select it
4. The application will load and display all configuration options

### Editing Bag Configuration
- **Drop Sections Tab**: Configure main bag settings like drop rates, set item options, and party settings
- **Class Restrictions Tab**: Set which character classes can access the bag
- **Item Drops Tab**: Manage individual item drop configurations
- **Settings Tab**: Configure application preferences

### Auto-Save Feature
The application automatically saves your configuration changes when you switch between tabs, ensuring no work is lost.

### Saving Changes
- Use **File → Save** or press **Ctrl+S** to save changes
- Use **File → Save As** to save to a new location

## 📁 File Structure

```
ItemBagEditor/
├── Models/              # Data models for ItemBag configurations
├── Services/            # Business logic and registry services
├── MainWindow.xaml      # Main application interface
├── MainWindow.xaml.cs   # Main window logic
├── SettingsDialog.xaml  # Settings dialog interface
├── EditItemDialog.xaml  # Item editing dialog
└── ItemBagEditor.csproj # Project configuration
```

## ⚙️ Configuration

### Application Settings
- **Item List Path**: Path to your item list configuration file
- **File Logging**: Enable/disable detailed logging to files
- **Registry Location**: `HKEY_CURRENT_USER\SOFTWARE\Inguz\ItemBagEditor`

### ItemBag XML Structure
The application supports the standard ItemBag XML format with these main elements:
- `<BagConfig>`: Main bag configuration with attributes like Name, ItemRate, SetItemRate, etc.
- `<SummonBook>`: Summon book drop settings
- `<AddCoin>`: Coin drop configuration
- `<Ruud>`: Ruud drop settings
- `<DropSection>`: Individual item drop configurations
- `<DropAllow>`: Class restriction settings

## 🔧 Development

### Prerequisites
- Visual Studio 2022 or VS Code
- .NET 6.0 SDK
- Git

### Building
```bash
# Standard build
dotnet build

# Release build
dotnet build --configuration Release

# Windows 7 compatible build
npm run build:win7
```

### Project Structure
- **WPF Application**: Uses MVVM pattern with code-behind
- **Dependency Injection**: Services are injected into the main window
- **Logging**: Serilog integration for application diagnostics
- **Registry Integration**: Windows Registry for persistent settings

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 🐛 Bug Reports

If you encounter any issues:
1. Check the [Issues](https://github.com/inguz-BA/itembageditor/issues) page
2. Create a new issue with detailed information about the problem
3. Include your ItemBag XML file (if relevant) and error messages

## 📞 Support

For support and questions:
- Create an issue on GitHub
- Check the application logs for detailed error information
- Ensure your ItemBag XML files follow the expected format

## 🔄 Version History

- **v1.0.1** - Current version with registry-based settings and auto-save functionality
- **v1.0.0** - Initial release with basic ItemBag editing capabilities

---

**Note**: This application is designed for MU Online private server administrators. Make sure you have the necessary permissions to modify server configuration files.
