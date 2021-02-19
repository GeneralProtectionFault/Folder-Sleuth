using System.Collections.Generic;
using System.IO;
using System.Windows;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
namespace Folder_Sleuth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<FileInfo> foundFiles = new List<FileInfo>();
        private List<DirectoryInfo> foundDirectories = new List<DirectoryInfo>();

        private List<string> stringsToSearch = new List<string>();
        private List<string> foundStrings = new List<string>();

        private List<FileInfo> filesInFolders = new List<FileInfo>();
        private List<DirectoryInfo> directoriesInFolders = new List<DirectoryInfo>();

        private readonly SynchronizationContext syncContext;

        private static int itemCount = 0;
        private static int itemProgress = 0;

        private static bool currentlySearching = false;

        public MainWindow()
        {
            InitializeComponent();
            syncContext = SynchronizationContext.Current;
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (currentlySearching)
                return;

            currentlySearching = true;

            txtFound.Text = "";
            txtNotFound.Text = "";
            foundFiles.Clear();
            foundDirectories.Clear();
            filesInFolders.Clear();
            directoriesInFolders.Clear();
            foundStrings.Clear();

            // Store the strings we're searching for
            stringsToSearch = txtSearch.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            // Variables to indicate progress bar %
            itemCount = 0;
            itemProgress = 0;
            progressBar.Value = 0;


            if (String.IsNullOrEmpty(txtFolder.Text.Trim()))
            {
                MessageBox.Show("Please specify a location to search");
                return;
            }


            // Search the designated folder for any files CONTAINING any of the strings
            if (!Directory.Exists(txtFolder.Text.Trim()))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }

            

            DirectoryInfo dir = new DirectoryInfo(txtFolder.Text.Trim());
            
            // Set search type depending on if box is checked
            SearchOption searchOption;

            if (chkSubfolders.IsChecked == true)
                searchOption = SearchOption.AllDirectories;
            else
                searchOption = SearchOption.TopDirectoryOnly;
            
                
            // FILE SEARCH
            if (btnFileSearch.IsChecked == true)
            {
                // First, just get all the files into a variable
                foreach (var file in dir.GetFiles("*", searchOption))
                {
                    txtStatusBar.Text = $"Found {file.Name}";

                    await Task.Run(() =>
                    {
                        filesInFolders.Add(file);
                        itemCount++;
                    });


                }

                progressBar.Maximum = itemCount;

                foreach (var file in filesInFolders)
                {
                    progressBar.Value = itemProgress;

                    // Check this file against ALL of the possible strings we're searching on
                    foreach (var item in stringsToSearch)
                    {
                        txtStatusBar.Text = $"Checking {file} for a string match";

                        await Task.Run(() =>
                        {
                            if (file.Name.ToUpper().Contains(item.ToUpper()))
                            {
                                UpdateFound(item + " - " + file.Name);
                                foundFiles.Add(file);
                                foundStrings.Add(item);
                            }
                        });
                    }

                    itemProgress++;
                }

                progressBar.Value = progressBar.Maximum;


                txtStatusBar.Text = "Checking for unmatched strings...";

                await Task.Run(() =>
                {
                // 
                foreach (var item in stringsToSearch)
                    {
                        if (!foundStrings.Contains(item))
                            UpdateNotFound(item);
                    }
                });
            }

            // FOLDER SEARCH
            else if (btnFolderSearch.IsChecked == true)
            {
                // First, just get all the files into a variable
                foreach (var directory in dir.GetDirectories("*", searchOption))
                {
                    txtStatusBar.Text = $"Found {directory.Name}";

                    await Task.Run(() =>
                    {
                        directoriesInFolders.Add(directory);
                        itemCount++;
                    });


                }


                progressBar.Maximum = itemCount;

                foreach (var directory in directoriesInFolders)
                {
                    progressBar.Value = itemProgress;

                    // Check this file against ALL of the possible strings we're searching on
                    foreach (var item in stringsToSearch)
                    {
                        txtStatusBar.Text = $"Checking {directory} for a string match";

                        await Task.Run(() =>
                        {
                            if (directory.Name.ToUpper().Contains(item.ToUpper()) 
                            //&& !txtFound.Text.Contains(directory.Name)
                            )
                            {
                                UpdateFound(item + " - " + directory.Name);
                                foundDirectories.Add(directory);
                                foundStrings.Add(item);
                            }
                        });
                    }

                    itemProgress++;
                }

                progressBar.Value = progressBar.Maximum;


                txtStatusBar.Text = "Checking for unmatched strings...";

                await Task.Run(() =>
                {
                    
                    foreach (var item in stringsToSearch)
                    {
                        if (!foundStrings.Contains(item))
                            UpdateNotFound(item);
                    }
                });
            }

            txtStatusBar.Text = "";

            currentlySearching = false;
        }


        private void UpdateFound(string message)
        {
            syncContext.Post(new SendOrPostCallback(obj =>
            {
                txtFound.AppendText((string)obj + "\n");
            }), message);
        }

        private void UpdateNotFound(string message)
        {
            syncContext.Post(new SendOrPostCallback(obj =>
            {
                txtNotFound.AppendText((string)obj + "\n");
            }), message);
        }


        /// <summary>
        /// Move the files matching strings to a designated folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnMove_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(txtDestinationFolder.Text))
            {
                MessageBox.Show("Please enter a destination to move the files/directories to below.");
                return;
            }

            if (!Directory.Exists(txtDestinationFolder.Text))
            {
                MessageBox.Show("Invalid destination specified below.");
                return;
            }


            MessageBoxResult result = MessageBox.Show("Are you sure?\n" +
            "This will move all the discovered files to the location below.","Confirm File/Directory Move",MessageBoxButton.YesNo);

            if (result == MessageBoxResult.No)
                return;

            if (btnFileSearch.IsChecked == true)
            {
                foreach (var item in foundFiles)
                {
                    txtStatusBar.Text = $"Moving {item}";

                    await Task.Run(() =>
                    {
                        MoveFileWhileUIBusy(item);
                    });
                }

                txtStatusBar.Text = "Done!";
            }


            else if (btnFolderSearch.IsChecked == true)
            {
                foreach (var item in foundDirectories)
                {
                    txtStatusBar.Text = $"Moving {item}";

                    await Task.Run(() =>
                    {
                        MoveFolderWhileUIBusy(item);
                    });
                }

                txtStatusBar.Text = "Done!";
            }
        }


        private void MoveFileWhileUIBusy(FileInfo file)
        {
            syncContext.Post(new SendOrPostCallback(obj =>
            {
                // Path.Combine seems to screw up here...this slash is important, so....
                string destFolderPath = txtDestinationFolder.Text;
                if (!destFolderPath.EndsWith('\\'))
                    destFolderPath = destFolderPath + '\\';

                try
                {
                    FileInfo theFile = (FileInfo)obj;
                    File.Move(theFile.FullName, destFolderPath + theFile.Name);
                }
                catch (Exception ex)
                {
                    LogError($"Error Moving {file} - {ex.Message}");
                }
            }), file);
        }

        private void MoveFolderWhileUIBusy(DirectoryInfo directory)
        {
            syncContext.Post(new SendOrPostCallback(obj =>
            {
                try
                {
                    // Path.Combine seems to screw up here...this slash is important, so....
                    string destFolderPath = txtDestinationFolder.Text;
                    if (!destFolderPath.EndsWith('\\'))
                        destFolderPath = destFolderPath + '\\';

                    DirectoryInfo theDirectory = (DirectoryInfo)obj;

                    // Will not work across volumes :(
                    // Directory.Move(theDirectory.FullName, txtDestinationFolder.Text + theDirectory.Name);

                    // Create the destination directory
                    var newDirectory = Directory.CreateDirectory(destFolderPath + theDirectory.Name);
                    
                    foreach (FileInfo file in theDirectory.GetFiles())
                    {
                        File.Copy(file.FullName, newDirectory.FullName + '\\' + file.Name);
                    }

                    Directory.Delete(theDirectory.FullName, true);
                }
                catch (Exception ex)
                {
                    LogError($"Error Moving {directory} - {ex.Message}");
                }
            }), directory);
        }



        internal void LogError(string error)
        {
            txtErrors.Text += error;
        }

        private void txtErrors_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txtErrors.ScrollToEnd();
        }
    }
}
