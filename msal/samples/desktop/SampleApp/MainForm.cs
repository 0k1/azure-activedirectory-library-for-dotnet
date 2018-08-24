﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;

namespace SampleApp
{
    public partial class MainForm : Form
    {
        private static readonly MsalAuthHelper _msalHelper = new MsalAuthHelper("11744750-bfe5-4818-a1c0-655455f68fa7");
        private static IAccount account = null;
        private static string token;
        public MainForm()
        {
            InitializeComponent();
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            account = _msalHelper.Application.GetAccountsAsync().Result.FirstOrDefault();
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

            signInPage.BackColor = Color.FromArgb(255, 67, 143, 255);
        }

        private async void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((sender as TabControl).TabIndex == 1)
            {
                string token = await _msalHelper.GetTokenForCurrentAccountAsync(new[] { "user.read" }, account)
                    .ConfigureAwait(false);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            AcquireTokenAsync().Wait();
            UpdateResponse(token);
        }

        static async Task AcquireTokenAsync()
        {
            token = await _msalHelper.GetTokenForCurrentAccountAsync(new[] { "user.read" }, account).ConfigureAwait(false);
        }

        private void UpdateResponse(string token)
        {
            if (token != null)
            {
                tokenResultBox.Text = "Result:\n" + token;
            }
            else
            {
                tokenResultBox.Text = "Authentication failed. No access token returned.";
            }
        }

        private async void signOutButton1_Click(object sender, EventArgs e)
        {
            var accounts = await _msalHelper.Application.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    await _msalHelper.Application.RemoveAsync(accounts.FirstOrDefault());
                    tokenResultBox.Text = "User has signed-out";
                }
                catch (MsalException ex)
                {
                    tokenResultBox.Text = $"Error signing-out user: {ex.Message}";
                }
            }
        }
    }
}
