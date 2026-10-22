using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SmartLocker.Data;
using SmartLocker.Models;
using SmartLocker.Services;

namespace SmartLocker.Forms
{
    public class MainForm : Form
    {
        private readonly IParcelRepository repository;
        private readonly ParcelProcessingService processingService;
        private readonly PickupService pickupService;
        private readonly DatasetGenerator datasetGenerator;

        private TextBox txtRecipient;
        private ComboBox cmbSize;
        private ComboBox cmbService;
        private Button btnAddParcel;
        private Button btnGenerate;
        private Button btnStart;
        private Button btnStop;
        private TextBox txtPickupCode;
        private Button btnCollect;
        private Label lblCounts;
        private Label lblBySize;
        private Label lblStatus;
        private DataGridView gridParcels;
        private FlowLayoutPanel panelCompartments;

        public MainForm(IParcelRepository repository)
        {
            this.repository = repository;
            processingService = new ParcelProcessingService(repository);
            pickupService = new PickupService(repository);
            datasetGenerator = new DatasetGenerator(repository);

            processingService.ParcelStored += OnParcelStored;
            processingService.ProcessingFinished += OnProcessingFinished;

            BuildUi();
            RefreshAll();
        }

        private void BuildUi()
        {
            Text = "SmartLocker - parcel terminal";
            Size = new Size(1250, 760);
            MinimumSize = new Size(1100, 650);
            StartPosition = FormStartPosition.CenterScreen;

            GroupBox grpNew = new GroupBox { Text = "New parcel", Location = new Point(10, 10), Size = new Size(330, 170) };
            grpNew.Controls.Add(new Label { Text = "Recipient:", Location = new Point(10, 28), AutoSize = true });
            txtRecipient = new TextBox { Location = new Point(90, 25), Width = 225, MaxLength = 100 };
            grpNew.Controls.Add(txtRecipient);

            grpNew.Controls.Add(new Label { Text = "Size:", Location = new Point(10, 62), AutoSize = true });
            cmbSize = new ComboBox { Location = new Point(90, 59), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSize.Items.AddRange(new object[] { "Small", "Medium", "Large" });
            grpNew.Controls.Add(cmbSize);

            grpNew.Controls.Add(new Label { Text = "Service:", Location = new Point(10, 96), AutoSize = true });
            cmbService = new ComboBox { Location = new Point(90, 93), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbService.Items.AddRange(new object[] { "Standard", "Express" });
            grpNew.Controls.Add(cmbService);

            btnAddParcel = new Button { Text = "Add parcel", Location = new Point(90, 128), Size = new Size(120, 30) };
            btnAddParcel.Click += BtnAddParcel_Click;
            grpNew.Controls.Add(btnAddParcel);
            Controls.Add(grpNew);

            GroupBox grpProcess = new GroupBox { Text = "Dataset / processing", Location = new Point(350, 10), Size = new Size(200, 170) };
            btnGenerate = new Button { Text = "Generate dataset", Location = new Point(15, 25), Size = new Size(170, 35) };
            btnGenerate.Click += BtnGenerate_Click;
            btnStart = new Button { Text = "Start processing", Location = new Point(15, 72), Size = new Size(170, 35) };
            btnStart.Click += BtnStart_Click;
            btnStop = new Button { Text = "Stop processing", Location = new Point(15, 119), Size = new Size(170, 35), Enabled = false };
            btnStop.Click += BtnStop_Click;
            grpProcess.Controls.Add(btnGenerate);
            grpProcess.Controls.Add(btnStart);
            grpProcess.Controls.Add(btnStop);
            Controls.Add(grpProcess);

            GroupBox grpPickup = new GroupBox { Text = "Pickup", Location = new Point(560, 10), Size = new Size(200, 170) };
            grpPickup.Controls.Add(new Label { Text = "6-digit pickup code:", Location = new Point(15, 28), AutoSize = true });
            txtPickupCode = new TextBox { Location = new Point(15, 50), Width = 170, MaxLength = 6 };
            grpPickup.Controls.Add(txtPickupCode);
            btnCollect = new Button { Text = "Collect parcel", Location = new Point(15, 85), Size = new Size(170, 35) };
            btnCollect.Click += BtnCollect_Click;
            grpPickup.Controls.Add(btnCollect);
            Controls.Add(grpPickup);

            GroupBox grpCounts = new GroupBox { Text = "Counters", Location = new Point(770, 10), Size = new Size(450, 170) };
            lblCounts = new Label { Location = new Point(15, 25), Size = new Size(420, 45), Font = new Font(Font, FontStyle.Bold) };
            lblBySize = new Label { Location = new Point(15, 75), Size = new Size(420, 60) };
            lblStatus = new Label { Location = new Point(15, 140), Size = new Size(420, 22), ForeColor = Color.DarkBlue };
            grpCounts.Controls.Add(lblCounts);
            grpCounts.Controls.Add(lblBySize);
            grpCounts.Controls.Add(lblStatus);
            Controls.Add(grpCounts);

            gridParcels = new DataGridView
            {
                Location = new Point(10, 190),
                Size = new Size(740, 510),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            Controls.Add(gridParcels);

            GroupBox grpBoxes = new GroupBox
            {
                Text = "Compartments (green = free, red = occupied)",
                Location = new Point(760, 190),
                Size = new Size(460, 510),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right
            };
            panelCompartments = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            grpBoxes.Controls.Add(panelCompartments);
            Controls.Add(grpBoxes);

            FormClosing += MainForm_FormClosing;
        }

        private void RefreshAll()
        {
            try
            {
                List<Parcel> parcels = repository.GetAllParcels();
                LockerUnit lockerUnit = repository.LoadLockerUnit();

                gridParcels.DataSource = null;
                gridParcels.DataSource = parcels;

                int waiting = 0, stored = 0, collected = 0;
                foreach (Parcel p in parcels)
                {
                    if (p.Status == ParcelStatus.Waiting) waiting++;
                    else if (p.Status == ParcelStatus.Stored) stored++;
                    else collected++;
                }

                lblCounts.Text = "Waiting: " + waiting + "   Stored: " + stored + "   Collected: " + collected +
                                 "\nFree compartments: " + lockerUnit.CountFree() + " / " + lockerUnit.Compartments.Count +
                                 "   Total parcels: " + parcels.Count;

                string bySize = "";
                foreach (SizeOccupancy row in lockerUnit.GetOccupancyBySize())
                {
                    bySize += row.Size + ": " + row.Free + " free, " + row.Occupied + " occupied\n";
                }
                lblBySize.Text = bySize;

                DrawCompartments(lockerUnit);
            }
            catch (Exception ex)
            {
                MessageBox.Show("could not load data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DrawCompartments(LockerUnit lockerUnit)
        {
            panelCompartments.SuspendLayout();
            panelCompartments.Controls.Clear();

            foreach (LockerCompartment box in lockerUnit.Compartments)
            {
                string text = "Box " + box.Id + "\n" + box.Size + "\n";
                if (box.IsOccupied)
                {
                    text += "parcel " + box.OccupiedByParcelId;
                }
                else
                {
                    text += "free";
                }

                Label boxLabel = new Label
                {
                    Text = text,
                    Size = new Size(100, 70),
                    Margin = new Padding(4),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = box.IsOccupied ? Color.LightCoral : Color.LightGreen
                };
                panelCompartments.Controls.Add(boxLabel);
            }

            panelCompartments.ResumeLayout();
        }

        private void BtnAddParcel_Click(object sender, EventArgs e)
        {
            string recipient = txtRecipient.Text.Trim();
            if (recipient.Length == 0)
            {
                MessageBox.Show("please enter a recipient name", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbSize.SelectedItem == null)
            {
                MessageBox.Show("please choose a size", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cmbService.SelectedItem == null)
            {
                MessageBox.Show("please choose a service type", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ParcelSize size = (ParcelSize)Enum.Parse(typeof(ParcelSize), cmbSize.SelectedItem.ToString());
                Parcel parcel;
                if (cmbService.SelectedItem.ToString() == "Express")
                {
                    parcel = new ExpressParcel(recipient, size);
                }
                else
                {
                    parcel = new StandardParcel(recipient, size);
                }

                int newId = repository.AddParcel(parcel);
                lblStatus.Text = "parcel " + newId + " added";
                txtRecipient.Clear();
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("could not add parcel: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            int existing = repository.CountParcels();
            if (existing > 0)
            {
                DialogResult answer = MessageBox.Show(
                    "the database already has " + existing + " parcels.\nadd " + DatasetGenerator.ParcelCount + " more?",
                    "Generate dataset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                int created = datasetGenerator.Generate();
                lblStatus.Text = created + " parcels generated";
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("dataset generation failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!processingService.Start())
            {
                MessageBox.Show("processing is already running", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "processing...";
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            processingService.RequestStop();
            btnStop.Enabled = false;
            lblStatus.Text = "stopping after the current parcel...";
        }

        private void BtnCollect_Click(object sender, EventArgs e)
        {
            string code = txtPickupCode.Text.Trim();
            if (!PickupService.IsValidCodeFormat(code))
            {
                MessageBox.Show("the pickup code must be exactly 6 digits", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Parcel collected = pickupService.Collect(code);
                if (collected == null)
                {
                    MessageBox.Show("invalid or already collected pickup code", "Pickup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lblStatus.Text = "parcel " + collected.Id + " collected from box " + collected.CompartmentId;
                txtPickupCode.Clear();
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("collect failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnParcelStored(string message)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                lblStatus.Text = message;
                RefreshAll();
            }));
        }

        private void OnProcessingFinished(string message)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                lblStatus.Text = message;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                RefreshAll();
            }));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            processingService.ParcelStored -= OnParcelStored;
            processingService.ProcessingFinished -= OnProcessingFinished;
            processingService.RequestStop();
        }
    }
}
