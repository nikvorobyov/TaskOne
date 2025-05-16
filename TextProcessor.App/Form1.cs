using TextProcessor.Core;
using System.Diagnostics;

namespace TextProcessor.App;

public partial class Form1 : Form
{
    private TextFileProcessor? _processor;
    private ListBox inputFilesListBox;
    private TextBox outputDirectoryTextBox;
    private NumericUpDown threadsNumeric;
    private NumericUpDown minLengthNumeric;
    private CheckBox removePunctuationCheck;
    private TextBox statisticsBox;
    private Button processButton;
    private Button addFilesButton;
    private Button clearFilesButton;

    public Form1()
    {
        InitializeComponent();
        SetupCustomControls();
    }

    private void SetupCustomControls()
    {
        this.Text = "Text File Processor";
        this.Size = new Size(800, 1000);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        // Input files list
        var inputLabel = new Label
        {
            Text = "Input Files:",
            Location = new Point(20, 20),
            AutoSize = true
        };

        inputFilesListBox = new ListBox
        {
            Location = new Point(20, 40),
            Size = new Size(600, 150),
            SelectionMode = SelectionMode.MultiExtended
        };

        addFilesButton = new Button
        {
            Text = "Add Files...",
            Location = new Point(630, 40),
            Width = 100
        };

        clearFilesButton = new Button
        {
            Text = "Clear",
            Location = new Point(630, 70),
            Width = 100
        };

        // Output directory selection
        var outputLabel = new Label
        {
            Text = "Output Directory:",
            Location = new Point(20, 200),
            AutoSize = true
        };

        outputDirectoryTextBox = new TextBox
        {
            Location = new Point(20, 220),
            Width = 600,
            ReadOnly = true
        };

        var outputButton = new Button
        {
            Text = "Browse...",
            Location = new Point(630, 218),
            Width = 100
        };

        // Processing settings
        var settingsGroup = new GroupBox
        {
            Text = "Processing Settings",
            Location = new Point(20, 260),
            Size = new Size(710, 150)
        };

        var threadsLabel = new Label
        {
            Text = "Number of Threads:",
            Location = new Point(10, 30),
            AutoSize = true
        };

        threadsNumeric = new NumericUpDown
        {
            Location = new Point(150, 28),
            Minimum = 1,
            Maximum = 32,
            Value = Environment.ProcessorCount
        };

        var minLengthLabel = new Label
        {
            Text = "Minimum Word Length:",
            Location = new Point(10, 60),
            AutoSize = true
        };

        minLengthNumeric = new NumericUpDown
        {
            Location = new Point(150, 58),
            Minimum = 1,
            Maximum = 50,
            Value = 3
        };

        removePunctuationCheck = new CheckBox
        {
            Text = "Remove Punctuation",
            Location = new Point(20, 90),
            AutoSize = true,
            Checked = true
        };

        // Add controls to settings group
        settingsGroup.Controls.AddRange(new Control[]
        {
            threadsLabel, threadsNumeric,
            minLengthLabel, minLengthNumeric,
            removePunctuationCheck
        });

        // Statistics output
        statisticsBox = new TextBox
        {
            Location = new Point(20, 430),
            Size = new Size(710, 400),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };

        // Process button
        processButton = new Button
        {
            Text = "Process Files",
            Location = new Point(20, 850),
            Width = 710,
            Height = 30
        };

        // Add all controls to form
        this.Controls.AddRange(new Control[]
        {
            inputLabel, inputFilesListBox, addFilesButton, clearFilesButton,
            outputLabel, outputDirectoryTextBox, outputButton,
            settingsGroup,
            statisticsBox,
            processButton
        });

        // Wire up events
        addFilesButton.Click += AddFilesButton_Click;
        clearFilesButton.Click += ClearFilesButton_Click;
        outputButton.Click += OutputButton_Click;
        processButton.Click += ProcessButton_Click;
    }

    private void AddFilesButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select Input Files",
            Multiselect = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            foreach (string file in dialog.FileNames)
            {
                if (!inputFilesListBox.Items.Contains(file))
                {
                    inputFilesListBox.Items.Add(file);
                }
            }
        }
    }

    private void ClearFilesButton_Click(object? sender, EventArgs e)
    {
        inputFilesListBox.Items.Clear();
    }

    private void OutputButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select Output Directory"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            outputDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void ProcessButton_Click(object? sender, EventArgs e)
    {
        if (inputFilesListBox.Items.Count == 0)
        {
            MessageBox.Show("Please select at least one input file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrEmpty(outputDirectoryTextBox.Text))
        {
            MessageBox.Show("Please select output directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            // Disable controls during processing
            processButton.Enabled = false;
            addFilesButton.Enabled = false;
            clearFilesButton.Enabled = false;
            statisticsBox.Text = "Processing...";
            Application.DoEvents();

            var totalStopwatch = Stopwatch.StartNew();
            var results = new List<string>();
            var tasks = new List<Task<(string fileName, long processingTime)>>();

            // Create tasks for each file
            foreach (string inputFile in inputFilesListBox.Items)
            {
                string fileName = Path.GetFileName(inputFile);
                string outputFile = Path.Combine(outputDirectoryTextBox.Text, fileName);

                var processor = new TextFileProcessor(
                    (int)minLengthNumeric.Value,
                    removePunctuationCheck.Checked,
                    (int)threadsNumeric.Value);

                tasks.Add(Task.Run(async () =>
                {
                    await processor.ProcessFileAsync(inputFile, outputFile);
                    return (fileName, processor.ProcessingTime);
                }));
            }

            // Wait for all tasks to complete
            var completedTasks = await Task.WhenAll(tasks);

            // Process results
            foreach (var result in completedTasks)
            {
                results.Add($"Processed {result.fileName} in {result.processingTime}ms");
            }

            totalStopwatch.Stop();
            results.Add($"Total processing time: {totalStopwatch.ElapsedMilliseconds}ms");
            statisticsBox.Text = string.Join("  ", results);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            processButton.Enabled = true;
            addFilesButton.Enabled = true;
            clearFilesButton.Enabled = true;
        }
    }
}
