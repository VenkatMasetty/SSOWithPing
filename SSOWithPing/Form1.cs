using SSOWithPing.Helper;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SSOWithPing
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitializeTabs();
        }
        // Create a static class to manage the authentication state globally
        


        private void InitializeTabs()
        {
            //Setting up the tab names
            tabControl1.TabPages[0].Text = "User Data";
            tabControl1.TabPages[1].Text = "Settings";
            tabControl1.TabPages[2].Text = "Statistics";
        }
        private void SetupLogoutButton()
        {
            Button btnLogout = new Button();
            btnLogout.Text = "Logout";
            btnLogout.Location = new Point(10, 10); // Adjust location as needed
            btnLogout.Click += new EventHandler(btnLogout_Click);
            this.Controls.Add(btnLogout);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            AuthenticationState.IsAuthenticated = false; // Reset authentication state
            this.Close(); // Close the dashboard
            MessageBox.Show("You have been logged out."); // Optionally inform the user
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Check if the user is authenticated before showing the form
            if (!AuthenticationState.IsAuthenticated)
            {
                MessageBox.Show("Please log in to access the dashboard.");
                this.Close(); // Close the form if not authenticated
            }
            else
            {
                SetupLogoutButton();
            }
        }
    }
}
