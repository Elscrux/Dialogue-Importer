
using System.Collections.Generic;

namespace DialogueParser
{
    partial class Form1
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
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lineLabel = new System.Windows.Forms.Label();
            this.btnParseDocument = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.form1BindingSource = new System.Windows.Forms.BindingSource(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 85);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(472, 36);
            this.progressBar1.TabIndex = 0;
            // 
            // lineLabel
            // 
            this.lineLabel.Location = new System.Drawing.Point(12, 42);
            this.lineLabel.Name = "lineLabel";
            this.lineLabel.Size = new System.Drawing.Size(472, 36);
            this.lineLabel.TabIndex = 1;
            // 
            // btnParseDocument
            // 
            this.btnParseDocument.Location = new System.Drawing.Point(12, 12);
            this.btnParseDocument.Name = "btnParseDocument";
            this.btnParseDocument.Size = new System.Drawing.Size(128, 24);
            this.btnParseDocument.TabIndex = 2;
            this.btnParseDocument.Text = "Parse document";
            this.btnParseDocument.UseVisualStyleBackColor = true;
            this.btnParseDocument.Click += new System.EventHandler(this.btnParseDocument_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(348, 12);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(136, 21);
            this.comboBox1.TabIndex = 3;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // form1BindingSource
            // 
            this.form1BindingSource.DataSource = typeof(DialogueParser.Form1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 133);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.btnParseDocument);
            this.Controls.Add(this.lineLabel);
            this.Controls.Add(this.progressBar1);
            this.Name = "Form1";
            this.Text = "Dialogue Parser";
            ((System.ComponentModel.ISupportInitialize)(this.form1BindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        public void setLabel(string label) {
            lineLabel.Text = label;
        }

        public void incrementProgressBar() {
            progressBar1.Value++;
        }

        public void clearProgressBar() {
            progressBar1.Value = 0;
        }

        public void setProgressBarMax(int max) {
            progressBar1.Maximum = max;
        }

        public void initDropDownItems(string[] dialogueModes) {
            foreach (var dialogueMode in dialogueModes) {
                this.comboBox1.Items.Add(dialogueMode);
            }
            comboBox1.SelectedIndex = 0;
        }

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnParseDocument;
        private System.Windows.Forms.Label lineLabel;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.BindingSource form1BindingSource;
    }
}

