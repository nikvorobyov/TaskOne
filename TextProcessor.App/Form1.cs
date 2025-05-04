using TextProcessor.Core;

namespace TextProcessor.App;

public partial class Form1 : Form
{
    private TextFileProcessor? _processor;
    private TextBox inputTextBox;
    private TextBox outputTextBox;
    private NumericUpDown threadsNumeric;
    private NumericUpDown minLengthNumeric;
    private CheckBox removePunctuationCheck;
    private TextBox statisticsBox;
    private Button processButton;

    public Form1()
    {
        InitializeComponent();
        SetupCustomControls();
    }

    private void SetupCustomControls()
    {
        this.Text = "Text File Processor";
        this.Size = new Size(800, 600);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        // Input file selection
        var inputLabel = new Label
        {
            Text = "Input File:",
            Location = new Point(20, 20),
            AutoSize = true
        };

        inputTextBox = new TextBox
        {
            Location = new Point(20, 40),
            Width = 600,
            ReadOnly = true
        };

        var inputButton = new Button
        {
            Text = "Browse...",
            Location = new Point(630, 38),
            Width = 100
        };

        // Output file selection
        var outputLabel = new Label
        {
            Text = "Output File:",
            Location = new Point(20, 70),
            AutoSize = true
        };

        outputTextBox = new TextBox
        {
            Location = new Point(20, 90),
            Width = 600,
            ReadOnly = true
        };

        var outputButton = new Button
        {
            Text = "Browse...",
            Location = new Point(630, 88),
            Width = 100
        };

        // Processing settings
        var settingsGroup = new GroupBox
        {
            Text = "Processing Settings",
            Location = new Point(20, 130),
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
            Location = new Point(20, 300),
            Size = new Size(710, 200),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };

        // Process button
        processButton = new Button
        {
            Text = "Process File",
            Location = new Point(20, 520),
            Width = 710,
            Height = 30
        };

        // Add all controls to form
        this.Controls.AddRange(new Control[]
        {
            inputLabel, inputTextBox, inputButton,
            outputLabel, outputTextBox, outputButton,
            settingsGroup,
            statisticsBox,
            processButton
        });

        // Wire up events
        inputButton.Click += InputButton_Click;
        outputButton.Click += OutputButton_Click;
        processButton.Click += ProcessButton_Click;
    }

    private void InputButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select Input File"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            inputTextBox.Text = dialog.FileName;
        }
    }

    private void OutputButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select Output File"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            outputTextBox.Text = dialog.FileName;
        }
    }

    private async void ProcessButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(inputTextBox.Text) || string.IsNullOrEmpty(outputTextBox.Text))
        {
            MessageBox.Show("Please select both input and output files.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            // Disable controls during processing
            processButton.Enabled = false;
            statisticsBox.Text = "Processing...";
            Application.DoEvents();

            _processor = new TextFileProcessor(
                inputTextBox.Text,
                outputTextBox.Text,
                (int)minLengthNumeric.Value,
                removePunctuationCheck.Checked,
                (int)threadsNumeric.Value);

            await _processor.ProcessFileAsync();

            // Show statistics
            statisticsBox.Text = _processor.ProcessingTime.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            processButton.Enabled = true;
        }
    }
}
