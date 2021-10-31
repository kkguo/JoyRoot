
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
            this.btnUP = new System.Windows.Forms.Button();
            this.btnLeft = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnRight = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnUP
            // 
            this.btnUP.Location = new System.Drawing.Point(470, 169);
            this.btnUP.Name = "btnUP";
            this.btnUP.Size = new System.Drawing.Size(109, 107);
            this.btnUP.TabIndex = 0;
            this.btnUP.Text = "^";
            this.btnUP.UseVisualStyleBackColor = true;
            this.btnUP.Click += new System.EventHandler(this.btnUP_Click);
            // 
            // btnLeft
            // 
            this.btnLeft.Location = new System.Drawing.Point(368, 273);
            this.btnLeft.Name = "btnLeft";
            this.btnLeft.Size = new System.Drawing.Size(109, 102);
            this.btnLeft.TabIndex = 1;
            this.btnLeft.Text = "<";
            this.btnLeft.UseVisualStyleBackColor = true;
            // 
            // btnDown
            // 
            this.btnDown.Location = new System.Drawing.Point(470, 381);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(109, 102);
            this.btnDown.TabIndex = 2;
            this.btnDown.Text = "v";
            this.btnDown.UseVisualStyleBackColor = true;
            // 
            // btnRight
            // 
            this.btnRight.Location = new System.Drawing.Point(585, 273);
            this.btnRight.Name = "btnRight";
            this.btnRight.Size = new System.Drawing.Size(109, 102);
            this.btnRight.TabIndex = 3;
            this.btnRight.Text = ">";
            this.btnRight.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(92, 65);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(222, 65);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            // 
            // mainform
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1256, 692);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnRight);
            this.Controls.Add(this.btnDown);
            this.Controls.Add(this.btnLeft);
            this.Controls.Add(this.btnUP);
            this.Name = "mainform";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.mainform_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnUP;
        private System.Windows.Forms.Button btnLeft;
        private System.Windows.Forms.Button btnDown;
        private System.Windows.Forms.Button btnRight;
        private System.Windows.Forms.Button btnConnect;
    }
}

