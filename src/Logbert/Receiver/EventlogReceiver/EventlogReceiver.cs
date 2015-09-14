﻿#region Copyright © 2015 Couchcoding

// File:    EventlogReceiver.cs
// Package: Logbert
// Project: Logbert
// 
// The MIT License (MIT)
// 
// Copyright (c) 2015 Couchcoding
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;

using Com.Couchcoding.Logbert.Interfaces;
using Com.Couchcoding.Logbert.Logging;
using System.Diagnostics;

using Com.Couchcoding.Logbert.Controls;

namespace Com.Couchcoding.Logbert.Receiver.EventlogReceiver
{
  /// <summary>
  /// Implements a <see cref="ILogProvider"/> for the event log service.
  /// </summary>
  public class EventlogReceiver : ReceiverBase
  {
    #region Private Consts

    /// <summary>
    /// Defines the default name for the machine to receive messages from.
    /// </summary>
    private const string DEFAULT_MACHINE_NAME = ".";

    #endregion

    #region Private Fields

    /// <summary>
    /// Holds the <see cref="EventLog"/> instance that receives the messages.
    /// </summary>
    private EventLog mEventLog;

    /// <summary>
    /// Counts the received messages;
    /// </summary>
    private int mLogNumber;

    /// <summary>
    /// Holds the name of the log on the specified computer.
    /// </summary>
    private readonly string mLogName;

    /// <summary>
    /// Holds the computer on which the log exists.
    /// </summary>
    private readonly string mMachineName;

    /// <summary>
    /// Holds the source of event log entries.
    /// </summary>
    private readonly string mSourceName;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the name of the <see cref="ILogProvider"/>.
    /// </summary>
    public override string Name
    {
      get
      {
        return "Eventlog Receiver";
      }
    }

    /// <summary>
    /// Gets the description of the <see cref="ILogProvider"/>
    /// </summary>
    public override string Description
    {
      get
      {
        return string.Format(
            "{0} ({1} - {2})"
          , Name
          , mLogName
          , string.IsNullOrEmpty(mSourceName) ? "*" : mSourceName);
      }
    }

    /// <summary>
    /// Gets the filename for export of the received <see cref="LogMessage"/>s.
    /// </summary>
    public override string ExportFileName
    {
      get
      {
        return Description;
      }
    }

    /// <summary>
    /// Gets the settings <see cref="Control"/> of the <see cref="ILogProvider"/>.
    /// </summary>
    public override ILogSettingsCtrl Settings
    {
      get
      {
        return new EventlogReceiverSettings();
      }
    }

    /// <summary>
    /// Gets the columns to display of the <see cref="ILogProvider"/>.
    /// </summary>
    public override Dictionary<int, string> Columns
    {
      get
      {
        return new Dictionary<int, string>
        {
          { 0, "Number"      },
          { 1, "Level"       },
          { 2, "Timestamp"   },
          { 3, "Logger"      },
          { 4, "Category"    },
          { 5, "Username"    },
          { 6, "Instance ID" },
          { 7, "Message"     }
        };
      }
    }

    /// <summary>
    /// Determines whether this <see cref="ILogProvider"/> supports reloading of the content, ot not.
    /// </summary>
    public override bool SupportsReload
    {
      get
      {
        return false;
      }
    }

    /// <summary>
    /// Get the <see cref="Control"/> to display details about a selected <see cref="LogMessage"/>.
    /// </summary>
    public override ILogPresenter DetailsControl
    {
      get
      {
        return new EventLogDetailsControl();
      }
    }

    /// <summary>
    /// Gets the supported <see cref="LogLevel"/>s of the <see cref="ILogProvider"/>.
    /// </summary>
    public override LogLevel SupportedLevels
    {
      get
      {
        return LogLevel.Info    | 
               LogLevel.Warning | 
               LogLevel.Error;
      }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles the Entry Written event of the <see cref="EventLog"/> instance.
    /// </summary>
    private void EventLogEntryWritten(object sender, EntryWrittenEventArgs e)
    {
      if (string.IsNullOrEmpty(mSourceName) || Equals(e.Entry.Source, mSourceName))
      {
        LogMessage newLogMsg = new LogMessageEventlog(
            e.Entry
          , ++mLogNumber);

        if (mLogHandler != null)
        {
          mLogHandler.HandleMessage(newLogMsg);
        }
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
      return Name;
    }

    /// <summary>
    /// Intizializes the <see cref="ILogProvider"/>.
    /// </summary>
    /// <param name="logHandler">The <see cref="ILogHandler"/> that may handle incomming <see cref="LogMessage"/>s.</param>
    public override void Initialize(ILogHandler logHandler)
    {
      base.Initialize(logHandler);

      mEventLog = new EventLog(
          mLogName
        , string.IsNullOrEmpty(mMachineName.Trim()) ? DEFAULT_MACHINE_NAME : mMachineName
        , mSourceName);

      mEventLog.EntryWritten       += EventLogEntryWritten;
      mEventLog.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Shuts down the <see cref="ILogProvider"/> instance.
    /// </summary>
    public override void Shutdown()
    {
      if (mEventLog != null)
      {
        mEventLog.EnableRaisingEvents = false;
        mEventLog.EntryWritten       -= EventLogEntryWritten;

        mEventLog.Dispose();
      }

      base.Shutdown();
    }

    /// <summary>
    /// Gets the header used for the CSV file export.
    /// </summary>
    /// <returns></returns>
    public override string GetCsvHeader()
    {
      return "\"Number\","
           + "\"Level\","
           + "\"Timestamp\","
           + "\"Logger\","
           + "\"Category\","
           + "\"User Name\","
           + "\"Thread\","
           + "\"Message\""
           + Environment.NewLine;
    }

    /// <summary>
    /// Resets the <see cref="ILogProvider"/> instance.
    /// </summary>
    public override void Clear()
    {
      mLogNumber = 0;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new and empty instance of the <see cref="EventlogReceiver"/> class.
    /// </summary>
    public EventlogReceiver()
    {

    }

    /// <summary>
    /// Creates a new and configured instance of the <see cref="Log4NetUdpReceiver"/> class.
    /// </summary>
    /// <param name="logName">The name of the log on the specified computer.</param>
    /// <param name="machineName">The computer on which the log exists.</param>
    /// <param name="sourceName">The source of event log entries.</param>
    public EventlogReceiver(string logName, string machineName, string sourceName)
    {
      mLogName     = logName;
      mMachineName = machineName;
      mSourceName  = sourceName;
    }

    #endregion
  }
}