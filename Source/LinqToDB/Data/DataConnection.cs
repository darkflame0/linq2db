﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

using JetBrains.Annotations;
using LinqToDB.Common.Internal;

namespace LinqToDB.Data
{
	using Async;
	using Common;
	using Configuration;
	using DataProvider;
	using Expressions;
	using Mapping;
	using RetryPolicy;

	/// <summary>
	/// Implements persistent database connection abstraction over different database engines.
	/// Could be initialized using connection string name or connection string,
	/// or attached to existing connection or transaction.
	/// </summary>
	[PublicAPI]
	public partial class DataConnection : IDataContext, ICloneable
	{
		#region .ctor

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property.
		/// </summary>
		public DataConnection() : this(new LinqToDBConnectionOptionsBuilder())
		{}

		/// <summary>
		/// Creates database connection object that uses default connection configuration from <see cref="DefaultConfiguration"/> property and provided mapping schema.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(MappingSchema mappingSchema) : this(new LinqToDBConnectionOptionsBuilder().UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration and mapping schema.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of null, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(string? configurationString, MappingSchema mappingSchema)
			: this(new LinqToDBConnectionOptionsBuilder().UseConfigurationString(configurationString ?? DefaultConfiguration!).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses provided connection configuration.
		/// </summary>
		/// <param name="configurationString">Name of database connection configuration to use with this connection.
		/// In case of <c>null</c>, configuration from <see cref="DefaultConfiguration"/> property will be used.</param>
		public DataConnection(string? configurationString)
			: this(new LinqToDBConnectionOptionsBuilder().UseConfigurationString(configurationString ?? DefaultConfiguration!))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
				string        providerName,
				string        connectionString,
				MappingSchema mappingSchema)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnectionString(providerName, connectionString).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection string.
		/// </summary>
		/// <param name="providerName">Name of database provider to use with this connection. <see cref="ProviderName"/> class for list of providers.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataConnection(
			string providerName,
			string connectionString)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnectionString(providerName, connectionString))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection string and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider dataProvider,
			string        connectionString,
			MappingSchema mappingSchema)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnectionString(dataProvider, connectionString).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection string.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionString">Database connection string to use for connection with database.</param>
		public DataConnection(
			IDataProvider dataProvider,
			string        connectionString)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnectionString(dataProvider, connectionString))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection factory and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionFactory">Database connection factory method.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider       dataProvider,
			Func<DbConnection> connectionFactory,
			MappingSchema       mappingSchema)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnectionFactory(dataProvider, connectionFactory).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection factory.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connectionFactory">Database connection factory method.</param>
		public DataConnection(
			IDataProvider       dataProvider,
			Func<DbConnection> connectionFactory)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnectionFactory(dataProvider, connectionFactory))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, connection and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection,
			MappingSchema mappingSchema)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnection(dataProvider, connection).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <remarks>
		/// <paramref name="connection"/> would not be disposed.
		/// </remarks>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection)
			: this(dataProvider, connection, false)
		{

		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and connection.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="connection">Existing database connection to use.</param>
		/// <param name="disposeConnection">If true <paramref name="connection"/> would be disposed on DataConnection disposing.</param>
		public DataConnection(
			IDataProvider dataProvider,
			DbConnection  connection,
			bool          disposeConnection)
			: this(new LinqToDBConnectionOptionsBuilder().UseConnection(dataProvider, connection, disposeConnection))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider, transaction and mapping schema.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="transaction">Existing database transaction to use.</param>
		/// <param name="mappingSchema">Mapping schema to use with this connection.</param>
		public DataConnection(
			IDataProvider  dataProvider,
			DbTransaction transaction,
			MappingSchema  mappingSchema)
			: this(new LinqToDBConnectionOptionsBuilder().UseTransaction(dataProvider, transaction).UseMappingSchema(mappingSchema))
		{
		}

		/// <summary>
		/// Creates database connection object that uses specified database provider and transaction.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation to use with this connection.</param>
		/// <param name="transaction">Existing database transaction to use.</param>
		public DataConnection(
			IDataProvider  dataProvider,
			DbTransaction transaction)
			: this(new LinqToDBConnectionOptionsBuilder().UseTransaction(dataProvider, transaction))
		{
		}

		private DataConnection(LinqToDBConnectionOptionsBuilder builder) : this(builder.Build())
		{
		}

		/// <summary>
		/// Creates database connection object that uses a <see cref="LinqToDBConnectionOptions"/> to configure the connection.
		/// </summary>
		/// <param name="options">Options, setup ahead of time.</param>
		public DataConnection(LinqToDBConnectionOptions options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));

			if (!options.IsValidConfigForConnectionType(this))
				throw new LinqToDBException(
					$"Improper options type used to create DataConnection {GetType()}, try creating a public constructor calling base and accepting type {nameof(LinqToDBConnectionOptions)}<{GetType().Name}>");

			InitConfig();

			DbConnection?  localConnection  = null;
			DbTransaction? localTransaction = null;

			switch (options.SetupType)
			{
				case ConnectionSetupType.ConfigurationString:
				case ConnectionSetupType.DefaultConfiguration:
				{
					ConfigurationString = options.ConfigurationString ?? DefaultConfiguration;

					if (ConfigurationString == null)
						throw new LinqToDBException("Configuration string is not provided.");

					var ci = GetConfigurationInfo(ConfigurationString);

					DataProvider     = ci.DataProvider;
					ConnectionString = ci.ConnectionString;
					MappingSchema    = DataProvider.MappingSchema;

					break;
				}
				case ConnectionSetupType.ConnectionString:
				{
					if (options.ProviderName == null && options.DataProvider == null)
						throw new LinqToDBException("DataProvider was not specified");

					IDataProvider? dataProvider;

					if (options.ProviderName != null)
					{
						if (!_dataProviders.TryGetValue(options.ProviderName, out dataProvider))
							dataProvider = GetDataProvider(options.ProviderName, options.ConnectionString!);

						if (dataProvider == null)
							throw new LinqToDBException($"DataProvider '{options.ProviderName}' not found.");
					}
					else
						dataProvider = options.DataProvider!;

					DataProvider     = dataProvider;
					ConnectionString = options.ConnectionString;
					MappingSchema    = DataProvider.MappingSchema;

					break;
				}
				case ConnectionSetupType.ConnectionFactory:
				{
					//copy to tmp variable so that if the factory in options gets changed later we will still use the old one
					//is this expected?
					var originalConnectionFactory = options.ConnectionFactory!;

					_connectionFactory = () =>
					{
						var connection = originalConnectionFactory();
						return connection;
					};

					DataProvider  = options.DataProvider!;
					MappingSchema = DataProvider.MappingSchema;

					break;
				}
				case ConnectionSetupType.Connection:
				{
					localConnection    = options.DbConnection;
					_disposeConnection = options.DisposeConnection;

					DataProvider  = options.DataProvider!;
					MappingSchema = DataProvider.MappingSchema;
					break;
				}
				case ConnectionSetupType.Transaction:
				{
					localConnection    = options.DbTransaction!.Connection;
					localTransaction   = options.DbTransaction;

					_closeTransaction  = false;
					_closeConnection   = false;
					_disposeConnection = false;

					DataProvider  = options.DataProvider!;
					MappingSchema = DataProvider.MappingSchema;

					break;
				}
				default:
					throw new NotImplementedException($"SetupType: {options.SetupType}");
			}

			RetryPolicy = Configuration.RetryPolicy.Factory != null
				? Configuration.RetryPolicy.Factory(this)
				: null;

			if (options.DataProvider != null)
			{
				DataProvider  = options.DataProvider;
				MappingSchema = DataProvider.MappingSchema;
			}

			if (options.MappingSchema != null)
			{
				AddMappingSchema(options.MappingSchema);
			}
			else if (Configuration.Linq.EnableAutoFluentMapping)
			{
				MappingSchema = new (MappingSchema);
			}

			if (options.OnTrace != null)
			{
				OnTraceConnection = options.OnTrace;
			}

			if (options.TraceLevel != null)
			{
				TraceSwitchConnection = new TraceSwitch("DataConnection", "DataConnection trace switch")
				{
					Level = options.TraceLevel.Value
				};
			}

			if (options.WriteTrace != null)
			{
				WriteTraceLineConnection = options.WriteTrace;
			}

			if (options.Interceptors != null)
			{
				foreach (var interceptor in options.Interceptors)
					AddInterceptor(interceptor);
			}

			if (localConnection != null)
			{
				_connection = localConnection is IAsyncDbConnection asyncDbConnection
					? asyncDbConnection
					: AsyncFactory.Create(localConnection);
			}

			if (localTransaction != null)
			{
				TransactionAsync = AsyncFactory.Create(localTransaction);
			}

			DataProvider.InitContext(this);
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Database configuration name (connection string name).
		/// </summary>
		public string?       ConfigurationString { get; private set; }
		/// <summary>
		/// Database provider implementation for specific database engine.
		/// </summary>
		public IDataProvider DataProvider        { get; private set; }
		/// <summary>
		/// Database connection string.
		/// </summary>
		public string?       ConnectionString    { get; private set; }
		/// <summary>
		/// Retry policy for current connection.
		/// </summary>
		public IRetryPolicy? RetryPolicy         { get; set; }

		private int  _msID;
		private int? _id;
		/// <summary>
		/// For internal use only.
		/// </summary>
		public  int   ID
		{
			get
			{
				if (!_id.HasValue || _msID != MappingSchema.ConfigurationID)
				{
					_id = new IdentifierBuilder(_msID = MappingSchema.ConfigurationID)
						.Add((ConfigurationString ?? ConnectionString ?? Connection.ConnectionString))
						.CreateID();
				}

				return _id.Value;
			}
		}

		private bool? _isMarsEnabled;
		/// <summary>
		/// Gets or sets status of Multiple Active Result Sets (MARS) feature. This feature available only for
		/// SQL Azure and SQL Server 2005+.
		/// </summary>
		public  bool   IsMarsEnabled
		{
			get
			{
				_isMarsEnabled ??= (bool)(DataProvider.GetConnectionInfo(this, "IsMarsEnabled") ?? false);

				return _isMarsEnabled.Value;
			}
			set => _isMarsEnabled = value;
		}

		/// <summary>
		/// Gets or sets default connection configuration name. Used by <see cref="DataConnection"/> by default and could be set automatically from:
		/// <para> - <see cref="ILinqToDBSettings.DefaultConfiguration"/>;</para>
		/// <para> - first non-global connection string name from <see cref="ILinqToDBSettings.ConnectionStrings"/>;</para>
		/// <para> - first non-global connection string name passed to <see cref="SetConnectionStrings"/> method.</para>
		/// </summary>
		/// <seealso cref="DefaultConfiguration"/>
		private static string? _defaultConfiguration;
		public  static string? DefaultConfiguration
		{
			get { InitConfig(); return _defaultConfiguration; }
			set => _defaultConfiguration = value;
		}

		/// <summary>
		/// Gets or sets name of default data provider, used by new connection if user didn't specified provider explicitly in constructor or in connection options.
		/// Initialized with value from <see cref="DefaultSettings"/>.<see cref="ILinqToDBSettings.DefaultDataProvider"/>.
		/// </summary>
		/// <seealso cref="DefaultConfiguration"/>
		private static string? _defaultDataProvider;
		public  static string? DefaultDataProvider
		{
			get { InitConfig(); return _defaultDataProvider; }
			set => _defaultDataProvider = value;
		}

		private static Action<TraceInfo> _onTrace = DefaultTrace;
		/// <summary>
		/// Sets trace handler, used for all new connections unless overriden in <see cref="LinqToDBConnectionOptions"/>
		/// defaults to calling <see cref="OnTraceInternal"/>.
		/// </summary>
		[Obsolete("Use OnTraceConnection instance property or LinqToDbConnectionOptions.OnTrace setting.")]
		public  static Action<TraceInfo>  OnTrace
		{
			get => _onTrace;
			set => _onTrace = value ?? DefaultTrace;
		}

		static void DefaultTrace(TraceInfo info)
		{
			info.DataConnection.OnTraceInternal(info);
		}

		/// <summary>
		/// Gets or sets trace handler, used for current connection instance.
		/// Configured on the connection builder using <see cref="LinqToDBConnectionOptionsBuilder.WithTracing(Action{TraceInfo})"/>.
		/// defaults to <see cref="OnTrace"/>.
		/// </summary>
		public Action<TraceInfo> OnTraceConnection { get; set; } = _onTrace;

		/// <summary>
		/// Writes the trace out using <see cref="WriteTraceLineConnection"/>.
		/// </summary>
		void OnTraceInternal(TraceInfo info)
		{
			switch (info.TraceInfoStep)
			{
				case TraceInfoStep.BeforeExecute:
					WriteTraceLineConnection(
						$"{info.TraceInfoStep}{Environment.NewLine}{info.SqlText}",
						TraceSwitchConnection.DisplayName,
						info.TraceLevel);
					break;

				case TraceInfoStep.AfterExecute:
					WriteTraceLineConnection(
						info.RecordsAffected != null
							? $"Query Execution Time ({info.TraceInfoStep}){(info.IsAsync ? " (async)" : "")}: {info.ExecutionTime}. Records Affected: {info.RecordsAffected}.\r\n"
							: $"Query Execution Time ({info.TraceInfoStep}){(info.IsAsync ? " (async)" : "")}: {info.ExecutionTime}\r\n",
						TraceSwitchConnection.DisplayName,
						info.TraceLevel);
					break;

				case TraceInfoStep.Error:
				{
					var sb = new StringBuilder();

					sb.Append(info.TraceInfoStep);

					for (var ex = info.Exception; ex != null; ex = ex.InnerException)
					{
						try
						{
							sb
								.AppendLine()
								.AppendLine($"Exception: {ex.GetType()}")
								.AppendLine($"Message  : {ex.Message}")
								.AppendLine(ex.StackTrace)
								;
						}
						catch
						{
							// Sybase provider could generate exception that will throw another exception when you
							// try to access Message property due to bug in AseErrorCollection.Message property.
							// There it tries to fetch error from first element of list without checking wether
							// list contains any elements or not
							sb
								.AppendLine()
								.AppendFormat("Failed while tried to log failure of type {0}", ex.GetType())
								;
						}
					}

					WriteTraceLineConnection(sb.ToString(), TraceSwitchConnection.DisplayName, info.TraceLevel);

					break;
				}

				case TraceInfoStep.MapperCreated:
				{
					var sb = new StringBuilder();

					sb.AppendLine(info.TraceInfoStep.ToString());

					if (Configuration.Linq.TraceMapperExpression && info.MapperExpression != null)
						sb.AppendLine(info.MapperExpression.GetDebugView());

					WriteTraceLineConnection(sb.ToString(), TraceSwitchConnection.DisplayName, info.TraceLevel);

					break;
				}

				case TraceInfoStep.Completed:
				{
					var sb = new StringBuilder();

					sb.Append($"Total Execution Time ({info.TraceInfoStep}){(info.IsAsync ? " (async)" : "")}: {info.ExecutionTime}.");

					if (info.RecordsAffected != null)
						sb.Append($" Rows Count: {info.RecordsAffected}.");

					sb.AppendLine();

					WriteTraceLineConnection(sb.ToString(), TraceSwitchConnection.DisplayName, info.TraceLevel);

					break;
				}
			}
		}

		private static TraceSwitch _traceSwitch = new ("DataConnection",
			"DataConnection trace switch",
#if DEBUG
			"Warning"
#else
				"Off"
#endif
		);

		/// <summary>
		/// Gets or sets global data connection trace options. Used for all new connections
		/// unless <see cref="LinqToDBConnectionOptionsBuilder.WithTraceLevel"/> is called on builder.
		/// defaults to off unless library was built in debug mode.
		/// <remarks>Should only be used when <see cref="TraceSwitchConnection"/> can not be used!</remarks>
		/// </summary>
		public static TraceSwitch TraceSwitch
		{
			// used by LoggingExtensions
			get => _traceSwitch;
			set => Volatile.Write(ref _traceSwitch, value);
		}

		/// <summary>
		/// Sets tracing level for data connections.
		/// </summary>
		/// <param name="traceLevel">Connection tracing level.</param>
		/// <remarks>Use <see cref="TraceSwitchConnection"/> when possible, configured via <see cref="LinqToDBConnectionOptionsBuilder.WithTraceLevel"/>.</remarks>
		public static void TurnTraceSwitchOn(TraceLevel traceLevel = TraceLevel.Info)
		{
			TraceSwitch = new TraceSwitch("DataConnection", "DataConnection trace switch", traceLevel.ToString());
		}


		private TraceSwitch? _traceSwitchConnection;

		/// <summary>
		/// gets or sets the trace switch,
		/// this is used by some methods to determine if <see cref="OnTraceConnection"/> should be called.
		/// defaults to <see cref="TraceSwitch"/>
		/// used for current connection instance.
		/// </summary>
		public TraceSwitch TraceSwitchConnection
		{
			get => _traceSwitchConnection ?? _traceSwitch;
			set => _traceSwitchConnection = value;
		}

		/// <summary>
		/// Trace function. By Default use <see cref="Debug"/> class for logging, but could be replaced to log e.g. to your log file.
		/// will be ignored if <see cref="LinqToDBConnectionOptionsBuilder.WriteTraceWith"/> is called on builder
		/// <para>First parameter contains trace message.</para>
		/// <para>Second parameter contains trace message category (<see cref="Switch.DisplayName"/>).</para>
		/// <para>Third parameter contains trace level for message (<see cref="TraceLevel"/>).</para>
		/// <seealso cref="TraceSwitch"/>
		/// <remarks>Should only not use to write trace lines, only use <see cref="WriteTraceLineConnection"/>.</remarks>
		/// </summary>
		public static Action<string?, string?, TraceLevel> WriteTraceLine = (message, category, level) => Debug.WriteLine(message, category);

		/// <summary>
		/// Gets the delegate to write logging messages for this connection.
		/// Defaults to <see cref="WriteTraceLine"/>.
		/// Used for the current instance.
		/// </summary>
		public Action<string?, string?, TraceLevel> WriteTraceLineConnection { get; } = WriteTraceLine;

		#endregion

		#region Configuration

		private static ILinqToDBSettings? _defaultSettings;

		/// <summary>
		/// Gets or sets default connection settings. By default contains settings from linq2db configuration section from configuration file (not supported by .Net Core).
		/// <seealso cref="ILinqToDBSettings"/>
		/// </summary>
		public static ILinqToDBSettings? DefaultSettings
		{
#if NETFRAMEWORK
			get => _defaultSettings ??= LinqToDBSection.Instance;
#else
			get => _defaultSettings;
#endif
			set => _defaultSettings = value;
		}

		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		static IDataProvider? FindProvider(
			string configuration,
			IEnumerable<KeyValuePair<string,IDataProvider>> ps,
			IDataProvider? defp)
		{
			foreach (var p in ps.OrderByDescending(kv => kv.Key.Length))
				if (configuration == p.Key || configuration.StartsWith(p.Key + '.'))
					return p.Value;

			foreach (var p in ps.OrderByDescending(kv => kv.Value.Name.Length))
				if (configuration == p.Value.Name || configuration.StartsWith(p.Value.Name + '.'))
					return p.Value;

			return defp;
		}

		static DataConnection()
		{
			// lazy registration of embedded providers using detectors
			AddProviderDetector(LinqToDB.DataProvider.Access    .AccessTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.DB2       .DB2Tools       .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Firebird  .FirebirdTools  .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Informix  .InformixTools  .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.MySql     .MySqlTools     .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Oracle    .OracleTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.PostgreSQL.PostgreSQLTools.ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SapHana   .SapHanaTools   .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SqlCe     .SqlCeTools     .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SQLite    .SQLiteTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.SqlServer .SqlServerTools .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.Sybase    .SybaseTools    .ProviderDetector);
			AddProviderDetector(LinqToDB.DataProvider.ClickHouse.ClickHouseTools.ProviderDetector);

			var section = DefaultSettings;

			if (section != null)
			{
				DefaultConfiguration = section.DefaultConfiguration;
				DefaultDataProvider  = section.DefaultDataProvider;

				foreach (var provider in section.DataProviders)
				{
					var dataProviderType = Type.GetType(provider.TypeName, true)!;
					var providerInstance = (IDataProviderFactory)Activator.CreateInstance(dataProviderType)!;

					if (!string.IsNullOrEmpty(provider.Name))
						AddDataProvider(provider.Name!, providerInstance.GetDataProvider(provider.Attributes));
				}
			}
		}

		static readonly List<Func<IConnectionStringSettings,string,IDataProvider?>> _providerDetectors = new();

		/// <summary>
		/// Registers database provider factory method.
		/// Factory accepts connection string settings and connection string. Could return <c>null</c>, if cannot create provider
		/// instance using provided options.
		/// </summary>
		/// <param name="providerDetector">Factory method delegate.</param>
		public static void AddProviderDetector(Func<IConnectionStringSettings,string,IDataProvider?> providerDetector)
		{
			_providerDetectors.Add(providerDetector);
		}

		/// <summary>
		/// Registers database provider factory method.
		/// Factory accepts connection string settings and connection string. Could return <c>null</c>, if cannot create provider
		/// instance using provided options.
		/// </summary>
		/// <param name="providerDetector">Factory method delegate.</param>
		public static void InsertProviderDetector(Func<IConnectionStringSettings,string,IDataProvider?> providerDetector)
		{
			_providerDetectors.Insert(0, providerDetector);
		}

		static void InitConnectionStrings()
		{
			if (DefaultSettings == null)
				return;

			foreach (var css in DefaultSettings.ConnectionStrings)
			{
				_configurations[css.Name] = new ConfigurationInfo(css);

				if (DefaultConfiguration == null && !css.IsGlobal /*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}
			}
		}

		static readonly object _initSyncRoot = new ();
		static          bool   _initialized;

		static void InitConfig()
		{
			lock (_initSyncRoot)
			{
				if (!_initialized)
				{
					_initialized = true;
					InitConnectionStrings();
				}
			}
		}

		static readonly ConcurrentDictionary<string,IDataProvider> _dataProviders = new ();

		/// <summary>
		/// Registers database provider implementation by provided unique name.
		/// </summary>
		/// <param name="providerName">Provider name, to which provider implementation will be mapped.</param>
		/// <param name="dataProvider">Database provider implementation.</param>
		public static void AddDataProvider(
			string        providerName,
			IDataProvider dataProvider)
		{
			if (providerName == null) throw new ArgumentNullException(nameof(providerName));
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

			if (string.IsNullOrEmpty(dataProvider.Name))
				throw new ArgumentException("dataProvider.Name cannot be empty.", nameof(dataProvider));

			_dataProviders[providerName] = dataProvider;
		}

		/// <summary>
		/// Registers database provider implementation using <see cref="IDataProvider.Name"/> name.
		/// </summary>
		/// <param name="dataProvider">Database provider implementation.</param>
		public static void AddDataProvider(IDataProvider dataProvider)
		{
			if (dataProvider == null) throw new ArgumentNullException(nameof(dataProvider));

			AddDataProvider(dataProvider.Name, dataProvider);
		}

		/// <summary>
		/// Returns database provider implementation, associated with provided connection configuration name.
		/// </summary>
		/// <param name="configurationString">Connection configuration name.</param>
		/// <returns>Database provider.</returns>
		public static IDataProvider GetDataProvider(string configurationString)
		{
			InitConfig();

			return GetConfigurationInfo(configurationString).DataProvider;
		}

		/// <summary>
		/// Returns database provider associated with provider name, configuration and connection string.
		/// </summary>
		/// <param name="providerName">Provider name.</param>
		/// <param name="configurationString">Connection configuration name.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Database provider.</returns>
		public static IDataProvider? GetDataProvider(
			string providerName,
			string configurationString,
			string connectionString)
		{
			InitConfig();

			return ConfigurationInfo.GetDataProvider(
				new ConnectionStringSettings(configurationString, connectionString, providerName),
				connectionString);
		}

		/// <summary>
		/// Returns database provider associated with provider name and connection string.
		/// </summary>
		/// <param name="providerName">Provider name.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <returns>Database provider.</returns>
		public static IDataProvider? GetDataProvider(
			string providerName,
			string connectionString)
		{
			InitConfig();

			return ConfigurationInfo.GetDataProvider(
				new ConnectionStringSettings(providerName, connectionString, providerName),
				connectionString);
		}

		/// <summary>
		/// Returns registered database providers.
		/// </summary>
		/// <returns>
		/// Returns registered providers collection.
		/// </returns>
		public static IReadOnlyDictionary<string, IDataProvider> GetRegisteredProviders() =>
			_dataProviders.ToDictionary(p => p.Key, p => p.Value);

		sealed class ConfigurationInfo
		{
			private readonly bool    _dataProviderSet;
			private readonly string? _configurationString;
			public ConfigurationInfo(string configurationString, string connectionString, IDataProvider? dataProvider)
			{
				ConnectionString     = connectionString;
				_dataProvider        = dataProvider;
				_dataProviderSet     = dataProvider != null;
				_configurationString = configurationString;
			}

			public ConfigurationInfo(IConnectionStringSettings connectionStringSettings)
			{
				ConnectionString = connectionStringSettings.ConnectionString;

				_connectionStringSettings = connectionStringSettings;
			}

			private string? _connectionString;
			public  string  ConnectionString
			{
				get => _connectionString!;
				set
				{
					if (!_dataProviderSet)
						_dataProvider = null;

					_connectionString = value;
				}
			}

			private readonly IConnectionStringSettings? _connectionStringSettings;

			private IDataProvider? _dataProvider;
			public  IDataProvider  DataProvider
			{
				get
				{
					var dataProvider = _dataProvider ??= GetDataProvider(_connectionStringSettings!, ConnectionString);

					if (dataProvider == null)
						throw new LinqToDBException($"DataProvider is not provided for configuration: {_configurationString}");

					return dataProvider;
				}
			}

			public static IDataProvider? GetDataProvider(IConnectionStringSettings css, string connectionString)
			{
				var configuration = css.Name;
				var providerName  = css.ProviderName;
				var dataProvider  = _providerDetectors.Select(d => d(css, connectionString)).FirstOrDefault(dp => dp != null);

				if (dataProvider == null)
				{
					IDataProvider? defaultDataProvider = null;

					if (DefaultDataProvider != null)
						_dataProviders.TryGetValue(DefaultDataProvider, out defaultDataProvider);

					if (string.IsNullOrEmpty(providerName))
						dataProvider = FindProvider(configuration, _dataProviders, defaultDataProvider);
					else if (!_dataProviders.TryGetValue(providerName!, out dataProvider) &&
					         !_dataProviders.TryGetValue(configuration, out dataProvider))
					{
						var providers = _dataProviders.Where(dp => dp.Value.ConnectionNamespace == providerName).ToList();

						dataProvider = providers.Count switch
						{
							0 => defaultDataProvider,
							1 => providers[0].Value,
							_ => FindProvider(configuration, providers, providers[0].Value),
						};
					}
				}

				if (dataProvider != null && DefaultConfiguration == null && !css.IsGlobal/*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}

				return dataProvider;
			}
		}

		static ConfigurationInfo GetConfigurationInfo(string? configurationString)
		{
			var key = configurationString ?? DefaultConfiguration;

			if (key == null)
				throw new LinqToDBException("Configuration string is not provided.");

			if (_configurations.TryGetValue(key, out var ci))
				return ci;

			throw new LinqToDBException($"Configuration '{configurationString}' is not defined.");
		}

		/// <summary>
		/// Register connection strings for use by data connection class.
		/// </summary>
		/// <param name="connectionStrings">Collection of connection string configurations.</param>
		public static void SetConnectionStrings(IEnumerable<IConnectionStringSettings> connectionStrings)
		{
			foreach (var css in connectionStrings)
			{
				_configurations[css.Name] = new ConfigurationInfo(css);

				if (DefaultConfiguration == null && !css.IsGlobal /*IsMachineConfig(css)*/)
				{
					DefaultConfiguration = css.Name;
				}
			}
		}

		static readonly ConcurrentDictionary<string,ConfigurationInfo> _configurations = new ();

		/// <summary>
		/// Register connection configuration with specified connection string and database provider implementation.
		/// </summary>
		/// <param name="configuration">Connection configuration name.</param>
		/// <param name="connectionString">Connection string.</param>
		/// <param name="dataProvider">Database provider. If not specified, will use provider, registered using <paramref name="configuration"/> value.</param>
		public static void AddConfiguration(
			string configuration,
			string connectionString,
			IDataProvider? dataProvider = null)
		{
			if (configuration    == null) throw new ArgumentNullException(nameof(configuration));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			if (dataProvider == null)
			{
				IDataProvider? defaultDataProvider = null;
				if (DefaultDataProvider != null)
					_dataProviders.TryGetValue(DefaultDataProvider, out defaultDataProvider);

				dataProvider = FindProvider(configuration, _dataProviders, defaultDataProvider);
			}

			var info = new ConfigurationInfo(
				configuration,
				connectionString,
				dataProvider);

			_configurations.AddOrUpdate(configuration, info, (s,i) => info);
		}

		internal static Lazy<IDataProvider> CreateDataProvider<T>()
			where T : IDataProvider, new()
		{
			return new(() =>
			{
				var provider = new T();
				AddDataProvider(provider);
				return provider;
			}, true);
		}

		public static void AddOrSetConfiguration(
			string configuration,
			string connectionString,
			string dataProvider)
		{
			if (configuration    == null) throw new ArgumentNullException(nameof(configuration));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
			if (dataProvider     == null) throw new ArgumentNullException(nameof(dataProvider));

			InitConfig();

			var info = new ConfigurationInfo(
				new ConnectionStringSettings(configuration, connectionString, dataProvider));

			_configurations.AddOrUpdate(configuration, info, (s,i) => info);
		}

		/// <summary>
		/// Sets connection string for specified connection name.
		/// </summary>
		/// <param name="configuration">Connection name.</param>
		/// <param name="connectionString">Connection string.</param>
		public static void SetConnectionString(
			string configuration,
			string connectionString)
		{
			if (configuration    == null) throw new ArgumentNullException(nameof(configuration));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			InitConfig();

			GetConfigurationInfo(configuration).ConnectionString = connectionString;
		}

		/// <summary>
		/// Returns connection string for specified connection name.
		/// </summary>
		/// <param name="configurationString">Connection name.</param>
		/// <returns>Connection string.</returns>
		public static string GetConnectionString(string configurationString)
		{
			InitConfig();

			return GetConfigurationInfo(configurationString).ConnectionString;
		}

		/// <summary>
		/// Returns connection string for specified configuration name or NULL.
		/// </summary>
		/// <param name="configurationString">Configuration.</param>
		/// <returns>Connection string or NULL.</returns>
		public static string? TryGetConnectionString(string? configurationString)
		{
			InitConfig();

			var key = configurationString ?? DefaultConfiguration;

			return key != null && _configurations.TryGetValue(key, out var ci) ? ci.ConnectionString : null;
		}

		#endregion

		#region Connection

		bool                 _closeConnection;
		bool                 _disposeConnection = true;
		bool                 _closeTransaction;
		IAsyncDbConnection?  _connection;

		readonly Func<DbConnection>? _connectionFactory;

		/// <summary>
		/// Gets underlying database connection, used by current connection object.
		/// </summary>
		public DbConnection Connection => EnsureConnection().Connection;

		internal IAsyncDbConnection EnsureConnection(bool connect = true)
		{
			CheckAndThrowOnDisposed();

			try
			{
				if (_connection == null)
				{
					DbConnection connection;
					if (_connectionFactory != null)
						connection = _connectionFactory();
					else
						connection = DataProvider.CreateConnection(ConnectionString!);

					_connection = AsyncFactory.Create(connection);

					if (RetryPolicy != null)
						_connection = new RetryingDbConnection(this, _connection, RetryPolicy);
				}
				else if (RetryPolicy != null && _connection is not RetryingDbConnection)
					_connection = new RetryingDbConnection(this, _connection, RetryPolicy);

				if (connect && _connection.State == ConnectionState.Closed)
				{
					_connectionInterceptor?.ConnectionOpening(new(this), _connection.Connection);

					_connection.Open();
					_closeConnection = true;

					_connectionInterceptor?.ConnectionOpened(new(this), _connection.Connection);
				}
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.Open, false)
					{
						TraceLevel = TraceLevel.Error,
						StartTime = DateTime.UtcNow,
						Exception = ex,
					});
				}

				throw;
			}


			return _connection;
		}

		/// <summary>
		/// Closes and dispose associated underlying database transaction/connection.
		/// </summary>
		public virtual void Close()
		{
			_dataContextInterceptor?.OnClosing(new (this));

			DisposeCommand();

			if (TransactionAsync != null && _closeTransaction)
			{
				TransactionAsync.Dispose();
				TransactionAsync = null;
			}

			if (_connection != null)
			{
				if (_disposeConnection)
				{
					_connection.Dispose();
					_connection = null;
				}
				else if (_closeConnection)
					_connection.Close();
			}

			_dataContextInterceptor?.OnClosed(new (this));
		}

		public FluentMappingBuilder GetFluentMappingBuilder()
		{
			if (MappingSchema.IsLockable)
				MappingSchema = new(MappingSchema);
			return MappingSchema.GetFluentMappingBuilder();
		}

		#endregion

		#region Command
		private DbCommand? _command;

		/// <summary>
		/// Gets current command instance if it exists or <c>null</c> otherwise.
		/// </summary>
		internal DbCommand? CurrentCommand => _command;

		/// <summary>
		/// Creates if needed and returns current command instance.
		/// </summary>
		internal DbCommand GetOrCreateCommand() => _command ??= CreateCommand();

		/// <summary>
		/// Contains text of last command, sent to database using current connection.
		/// </summary>
		public string? LastQuery { get; private set; }

		internal void InitCommand(CommandType commandType, string sql, DataParameter[]? parameters, IReadOnlyCollection<string>? queryHints, bool withParameters)
		{
			if (queryHints?.Count > 0)
			{
				var sqlProvider = DataProvider.CreateSqlBuilder(MappingSchema);
				sql             = sqlProvider.ApplyQueryHints(sql, queryHints);
			}

			_command = DataProvider.InitCommand(this, GetOrCreateCommand(), commandType, sql, parameters, withParameters);
		}

		internal void CommitCommandInit()
		{
			if (_commandInterceptor != null)
				_command = _commandInterceptor.CommandInitialized(new (this), _command!);

			LastQuery = _command!.CommandText;
		}

		private int? _commandTimeout;
		/// <summary>
		/// Gets or sets command execution timeout in seconds.
		/// Negative timeout value means that default timeout will be used.
		/// 0 timeout value corresponds to infinite timeout.
		/// By default timeout is not set and default value for current provider used.
		/// </summary>
		public  int   CommandTimeout
		{
			get => _commandTimeout ?? -1;
			set
			{
				if (value < 0)
				{
					// to reset to default timeout we dispose command because as command has no reset timeout API
					_commandTimeout = null;
					DisposeCommand();
				}
				else
				{
					_commandTimeout = value;
					if (_command != null)
						_command.CommandTimeout = value;
				}
			}
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// </summary>
		public DbCommand CreateCommand()
		{
			var command = EnsureConnection().CreateCommand();

			if (_commandTimeout.HasValue)
				command.CommandTimeout = _commandTimeout.Value;

			if (TransactionAsync != null)
				command.Transaction = Transaction;

			return command;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// </summary>
		public void DisposeCommand()
		{
			if (_command != null)
			{
				DataProvider.DisposeCommand(_command);
				_command = null;
			}
		}

		#region ExecuteNonQuery

		protected virtual int ExecuteNonQuery(DbCommand command)
		{
			if (_commandInterceptor == null)
				return command.ExecuteNonQuery();

			var result = _commandInterceptor.ExecuteNonQuery(new (this), command, Option<int>.None);

			return result.HasValue
				? result.Value
				: command.ExecuteNonQuery();
		}

		internal int ExecuteNonQuery()
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteNonQuery(CurrentCommand!);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteNonQuery, false)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				int ret;
				using (DataProvider.ExecuteScope(this))
					ret = ExecuteNonQuery(CurrentCommand!);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = CurrentCommand,
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = ret,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		internal int ExecuteNonQueryCustom(DbCommand command, Func<DbCommand, int> customExecute)
		{
			if (_commandInterceptor == null)
				return customExecute(command);

			// remove?
			var result = _commandInterceptor.ExecuteNonQuery(new (this), command, Option<int>.None);

			return result.HasValue
				? result.Value
				: customExecute(command);
		}

		internal int ExecuteNonQueryCustom(Func<DbCommand, int> customExecute)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteNonQueryCustom(CurrentCommand!, customExecute);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteNonQuery, false)
				{
					TraceLevel = TraceLevel.Info,
					Command = CurrentCommand,
					StartTime = now,
				});
			}

			try
			{
				int ret;
				using (DataProvider.ExecuteScope(this))
					ret = ExecuteNonQueryCustom(CurrentCommand!, customExecute);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel = TraceLevel.Info,
						Command = CurrentCommand,
						StartTime = now,
						ExecutionTime = sw.Elapsed,
						RecordsAffected = ret,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteNonQuery, false)
					{
						TraceLevel = TraceLevel.Error,
						Command = CurrentCommand,
						StartTime = now,
						ExecutionTime = sw.Elapsed,
						Exception = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteScalar

		protected virtual object? ExecuteScalar(DbCommand command)
		{
			var result = Option<object?>.None;

			if (_commandInterceptor != null)
				result = _commandInterceptor.ExecuteScalar(new (this), command, result);

			return result.HasValue
				? result.Value
				: command.ExecuteScalar();
		}

		object? ExecuteScalar()
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteScalar(CurrentCommand!);

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteScalar, false)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				object? ret;
				using (DataProvider.ExecuteScope(this))
					ret = ExecuteScalar(CurrentCommand!);

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteScalar, false)
					{
						TraceLevel     = TraceLevel.Info,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteScalar, false)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region ExecuteReader

		protected virtual DataReaderWrapper ExecuteReader(CommandBehavior commandBehavior)
		{
			var result = Option<DbDataReader>.None;

			if (_commandInterceptor != null)
				result = _commandInterceptor.ExecuteReader(new (this), _command!, commandBehavior, result);

			var rd = result.HasValue
				? result.Value
				: _command!.ExecuteReader(commandBehavior);

			if (_commandInterceptor != null)
				_commandInterceptor.AfterExecuteReader(new (this), _command!, commandBehavior, rd);

			var wrapper = new DataReaderWrapper(this, rd, _command!);

			_command = null;

			return wrapper;
		}

		DataReaderWrapper ExecuteReader()
		{
			return ExecuteDataReader(CommandBehavior.Default);
		}

		internal DataReaderWrapper ExecuteDataReader(CommandBehavior commandBehavior)
		{
			if (TraceSwitchConnection.Level == TraceLevel.Off)
				using (DataProvider.ExecuteScope(this))
					return ExecuteReader(GetCommandBehavior(commandBehavior));

			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (TraceSwitchConnection.TraceInfo)
			{
				OnTraceConnection(new TraceInfo(this, TraceInfoStep.BeforeExecute, TraceOperation.ExecuteReader, false)
				{
					TraceLevel     = TraceLevel.Info,
					Command        = CurrentCommand,
					StartTime      = now,
				});
			}

			try
			{
				DataReaderWrapper ret;

				using (DataProvider.ExecuteScope(this))
					ret = ExecuteReader(GetCommandBehavior(commandBehavior));

				if (TraceSwitchConnection.TraceInfo)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.AfterExecute, TraceOperation.ExecuteReader, false)
					{
						TraceLevel     = TraceLevel.Info,
						Command        = ret.Command,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
					});
				}

				return ret;
			}
			catch (Exception ex)
			{
				if (TraceSwitchConnection.TraceError)
				{
					OnTraceConnection(new TraceInfo(this, TraceInfoStep.Error, TraceOperation.ExecuteReader, false)
					{
						TraceLevel     = TraceLevel.Error,
						Command        = CurrentCommand,
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		/// <summary>
		/// Removes cached data mappers.
		/// </summary>
		public static void ClearObjectReaderCache()
		{
			CommandInfo.ClearObjectReaderCache();
		}

		#endregion

		#region Transaction
		/// <summary>
		/// Gets current transaction, associated with connection.
		/// </summary>
		public DbTransaction? Transaction => TransactionAsync?.Transaction;

		/// <summary>
		/// Async transaction wrapper over <see cref="Transaction"/>.
		/// </summary>
		internal IAsyncDbTransaction? TransactionAsync { get; private set; }

		/// <summary>
		/// Starts new transaction for current connection with default isolation level. If connection already has transaction, it will be rolled back.
		/// </summary>
		/// <returns>Database transaction object.</returns>
		public virtual DataConnectionTransaction BeginTransaction()
		{
			if (!DataProvider.TransactionsSupported)
				return new(this);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			TransactionAsync?.Dispose();

			var dataConnectionTransaction = TraceAction(
				this,
				TraceOperation.BeginTransaction,
				static _ => "BeginTransaction",
				default(object?),
				static (dataContext, _) =>
				{
			// Create new transaction object.
			//
					dataContext.TransactionAsync = dataContext.EnsureConnection().BeginTransaction();

					dataContext._closeTransaction = true;

			// If the active command exists.
					if (dataContext._command != null)
						dataContext._command.Transaction = dataContext.Transaction;

					return new DataConnectionTransaction(dataContext);
				});

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Starts new transaction for current connection with specified isolation level. If connection already have transaction, it will be rolled back.
		/// </summary>
		/// <param name="isolationLevel">Transaction isolation level.</param>
		/// <returns>Database transaction object.</returns>
		public virtual DataConnectionTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			if (!DataProvider.TransactionsSupported)
				return new(this);

			// If transaction is open, we dispose it, it will rollback all changes.
			//
			TransactionAsync?.Dispose();

			var dataConnectionTransaction = TraceAction(
				this,
				TraceOperation.BeginTransaction,
				static il => $"BeginTransaction({il})",
				isolationLevel,
				static (dataConnection, isolationLevel) =>
				{
			// Create new transaction object.
			//
					dataConnection.TransactionAsync = dataConnection.EnsureConnection().BeginTransaction(isolationLevel);

					dataConnection._closeTransaction = true;

			// If the active command exists.
					if (dataConnection._command != null)
						dataConnection._command.Transaction = dataConnection.Transaction;

					return new DataConnectionTransaction(dataConnection);
				});

			return dataConnectionTransaction;
		}

		/// <summary>
		/// Commits transaction (if any), associated with connection.
		/// </summary>
		public virtual void CommitTransaction()
		{
			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.CommitTransaction,
					static _ => "CommitTransaction",
					default(object?),
					static (dataConnection, _) =>
					{
						dataConnection.TransactionAsync!.Commit();

						if (dataConnection._closeTransaction)
				{
							dataConnection.TransactionAsync.Dispose();
							dataConnection.TransactionAsync = null;

							if (dataConnection._command != null)
								dataConnection._command.Transaction = null;
				}

						return true;
					});
			}
		}

		/// <summary>
		/// Rollbacks transaction (if any), associated with connection.
		/// </summary>
		public virtual void RollbackTransaction()
		{
			if (TransactionAsync != null)
			{
				TraceAction(
					this,
					TraceOperation.RollbackTransaction,
					static _ => "RollbackTransaction",
					default(object?),
					static (dataConnection, _) =>
					{
						dataConnection.TransactionAsync!.Rollback();

						if (dataConnection._closeTransaction)
						{
							dataConnection.TransactionAsync.Dispose();
							dataConnection.TransactionAsync = null;

							if (dataConnection._command != null)
								dataConnection._command.Transaction = null;
						}

						return true;
					});
			}
		}

		#endregion

		protected static TResult TraceAction<TContext, TResult>(
			DataConnection                          dataConnection,
			TraceOperation                          traceOperation,
			Func<TContext, string?>?                commandText,
			TContext                                context,
			Func<DataConnection, TContext, TResult> action)
		{
			var now       = DateTime.UtcNow;
			Stopwatch? sw = null;
			var sql       = dataConnection.TraceSwitchConnection.TraceInfo ? commandText?.Invoke(context) : null;

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				sw = Stopwatch.StartNew();
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute, traceOperation, false)
				{
					TraceLevel  = TraceLevel.Info,
					CommandText = sql,
					StartTime   = now,
				});
			}

			try
			{
				var actionResult = action(dataConnection, context);

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute, traceOperation, false)
					{
						TraceLevel    = TraceLevel.Info,
						CommandText   = sql,
						StartTime     = now,
						ExecutionTime = sw!.Elapsed
					});
				}

				return actionResult;
			}
			catch (Exception ex)
			{
				if (dataConnection.TraceSwitchConnection.TraceError)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.Error, traceOperation, false)
					{
						TraceLevel    = TraceLevel.Error,
						CommandText   = dataConnection.TraceSwitchConnection.TraceInfo ? sql : commandText?.Invoke(context),
						StartTime     = now,
						ExecutionTime = sw?.Elapsed,
						Exception     = ex,
					});
				}

				throw;
			}
		}

		#region MappingSchema

		/// <summary>
		/// Gets mapping schema, used for current connection.
		/// </summary>
		public  MappingSchema  MappingSchema { get; private set; }

		/// <summary>
		/// Gets or sets option to force inline parameter values as literals into command text. If parameter inlining not supported
		/// for specific value type, it will be used as parameter.
		/// </summary>
		public bool InlineParameters { get; set; }

		private List<string>? _queryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used for all queries, executed through current connection.
		/// </summary>
		public  List<string>  QueryHints => _queryHints ??= new List<string>();

		private List<string>? _nextQueryHints;
		/// <summary>
		/// Gets list of query hints (writable collection), that will be used only for next query, executed through current connection.
		/// </summary>
		public  List<string>  NextQueryHints => _nextQueryHints ??= new List<string>();

		/// <summary>
		/// Adds additional mapping schema to current connection.
		/// </summary>
		/// <remarks><see cref="DataConnection"/> will share <see cref="Mapping.MappingSchema"/> instances that were created by combining same mapping schemas.</remarks>
		/// <param name="mappingSchema">Mapping schema.</param>
		/// <returns>Current connection object.</returns>
		public DataConnection AddMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema = new (mappingSchema, MappingSchema);
			_id           = null;

			return this;
		}

		#endregion

		#region ICloneable Members

		DataConnection(string? configurationString, IDataProvider dataProvider, string? connectionString, DbConnection? connection, MappingSchema mappingSchema)
		{
			ConfigurationString = configurationString;
			DataProvider        = dataProvider;
			ConnectionString    = connectionString;
			_connection         = connection != null ? AsyncFactory.Create(connection) : null;
			MappingSchema       = mappingSchema;
		}

		/// <summary>
		/// Clones current connection.
		/// </summary>
		/// <returns>Cloned connection.</returns>
		public object Clone()
		{
			CheckAndThrowOnDisposed();

			var connection = _connection?.TryClone() ?? _connectionFactory?.Invoke();

			// https://github.com/linq2db/linq2db/issues/1486
			// when there is no ConnectionString and provider doesn't support connection cloning
			// try to get ConnectionString from _connection
			// will not work for providers that remove security information from connection string
			var connectionString = ConnectionString ?? (connection == null ? _connection?.ConnectionString : null);

			return new DataConnection(ConfigurationString, DataProvider, connectionString, connection, MappingSchema)
			{
				RetryPolicy                 = RetryPolicy,
				CommandTimeout              = CommandTimeout,
				InlineParameters            = InlineParameters,
				ThrowOnDisposed             = ThrowOnDisposed,
				OnTraceConnection         = OnTraceConnection,
				_queryHints                 = _queryHints?.Count > 0 ? _queryHints.ToList() : null,
				_commandInterceptor       = _commandInterceptor      .CloneAggregated(),
				_connectionInterceptor    = _connectionInterceptor   .CloneAggregated(),
				_dataContextInterceptor   = _dataContextInterceptor  .CloneAggregated(),
				_entityServiceInterceptor = _entityServiceInterceptor.CloneAggregated(),
			};
		}

		#endregion

		#region System.IDisposable Members

		protected bool  Disposed        { get; private set; }
		public    bool? ThrowOnDisposed { get; set; }

		protected void CheckAndThrowOnDisposed()
		{
			if (Disposed && (ThrowOnDisposed ?? Configuration.Data.ThrowOnDisposed))
				throw new ObjectDisposedException("DataConnection", "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		/// <summary>
		/// Disposes connection.
		/// </summary>
		public void Dispose()
		{
			Disposed = true;

			Close();
		}

		#endregion

		internal CommandBehavior GetCommandBehavior(CommandBehavior commandBehavior)
		{
			return DataProvider.GetCommandBehavior(commandBehavior);
		}
	}
}
