
namespace JoyRoot
{
    partial class mainform
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnConnect = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.listAvailibleRoot = new System.Windows.Forms.ListView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.btnStop = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnRight = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnLeft = new System.Windows.Forms.Button();
            this.btnUP = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.picColorSensor = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picColorSensor)).BeginInit();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(474, 23);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(2);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(111, 34);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Scan";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 579);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "label1";
            // 
            // colorDialog1
            // 
            this.colorDialog1.SolidColorOnly = true;
            // 
            // listAvailibleRoot
            // 
            this.listAvailibleRoot.CheckBoxes = true;
            this.listAvailibleRoot.HideSelection = false;
            this.listAvailibleRoot.Location = new System.Drawing.Point(474, 71);
            this.listAvailibleRoot.Name = "listAvailibleRoot";
            this.listAvailibleRoot.Size = new System.Drawing.Size(257, 259);
            this.listAvailibleRoot.TabIndex = 9;
            this.listAvailibleRoot.UseCompatibleStateImageBehavior = false;
            this.listAvailibleRoot.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listAvailibleRoot_ItemChecked);
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(64, 64);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.FlatAppearance.BorderSize = 0;
            this.btnStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStop.Image = global::JoyRoot.Properties.Resources.Stop;
            this.btnStop.Location = new System.Drawing.Point(234, 139);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(54, 56);
            this.btnStop.TabIndex = 8;
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.Image = global::JoyRoot.Properties.Resources.LED;
            this.pictureBox1.Location = new System.Drawing.Point(372, 139);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // btnRight
            // 
            this.btnRight.Enabled = false;
            this.btnRight.FlatAppearance.BorderSize = 0;
            this.btnRight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRight.Image = global::JoyRoot.Properties.Resources.TurnRight;
            this.btnRight.Location = new System.Drawing.Point(291, 139);
            this.btnRight.Margin = new System.Windows.Forms.Padding(2);
            this.btnRight.Name = "btnRight";
            this.btnRight.Size = new System.Drawing.Size(54, 56);
            this.btnRight.TabIndex = 3;
            this.btnRight.UseVisualStyleBackColor = true;
            this.btnRight.Click += new System.EventHandler(this.btnRight_Click);
            // 
            // btnDown
            // 
            this.btnDown.Enabled = false;
            this.btnDown.FlatAppearance.BorderSize = 0;
            this.btnDown.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDown.Image = global::JoyRoot.Properties.Resources.MoveBackward;
            this.btnDown.Location = new System.Drawing.Point(234, 197);
            this.btnDown.Margin = new System.Windows.Forms.Padding(2);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(54, 63);
            this.btnDown.TabIndex = 2;
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnLeft
            // 
            this.btnLeft.Enabled = false;
            this.btnLeft.FlatAppearance.BorderSize = 0;
            this.btnLeft.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLeft.Image = global::JoyRoot.Properties.Resources.TurnLeft;
            this.btnLeft.Location = new System.Drawing.Point(177, 139);
            this.btnLeft.Margin = new System.Windows.Forms.Padding(2);
            this.btnLeft.Name = "btnLeft";
            this.btnLeft.Size = new System.Drawing.Size(54, 56);
            this.btnLeft.TabIndex = 1;
            this.btnLeft.UseVisualStyleBackColor = true;
            this.btnLeft.Click += new System.EventHandler(this.btnLeft_Click);
            // 
            // btnUP
            // 
            this.btnUP.Enabled = false;
            this.btnUP.FlatAppearance.BorderSize = 0;
            this.btnUP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUP.Image = global::JoyRoot.Properties.Resources.MoveForward;
            this.btnUP.Location = new System.Drawing.Point(234, 71);
            this.btnUP.Margin = new System.Windows.Forms.Padding(2);
            this.btnUP.Name = "btnUP";
            this.btnUP.Size = new System.Drawing.Size(54, 66);
            this.btnUP.TabIndex = 0;
            this.btnUP.UseVisualStyleBackColor = true;
            this.btnUP.Click += new System.EventHandler(this.btnUP_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(484, 400);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(112, 35);
            this.button1.TabIndex = 10;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // picColorSensor
            // 
            this.picColorSensor.Location = new System.Drawing.Point(42, 299);
            this.picColorSensor.Name = "picColorSensor";
            this.picColorSensor.Size = new System.Drawing.Size(415, 31);
            this.picColorSensor.TabIndex = 11;
            this.picColorSensor.TabStop = false;
            // 
            // mainform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(780, 612);
            this.Controls.Add(this.picColorSensor);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listAvailibleRoot);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnRight);
            this.Controls.Add(this.btnDown);
            this.Controls.Add(this.btnLeft);
            this.Controls.Add(this.btnUP);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "mainform";
            this.Text = "JoyRoot";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.mainform_FormClosing);
            this.Load += new System.EventHandler(this.mainform_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picColorSensor)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnUP;
        private System.Windows.Forms.Button btnLeft;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnRight;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.ListView listAvailibleRoot;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.PictureBox picColorSensor;
    }
}

