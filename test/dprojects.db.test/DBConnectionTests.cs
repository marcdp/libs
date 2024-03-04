using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Db.Tests {

    public abstract class DBConnectionTests<T> where T : IDBConnection {

        //vars
        private readonly IDBConnection mDBConnection;


        //constructor
        public DBConnectionTests(string connectionString) {
            // ... initialize your test data here ...
            var services = new ServiceCollection();
            services.AddFactoryByUrl<IDBConnection>(cfg => {
                cfg.AddFactoriesFromAssembly(typeof(T).Assembly);    
            });
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IFactoryByUrl<IDBConnection>>();
            mDBConnection = factory.Create(connectionString);
        }

        //tests
        [Fact]
        public void Open_ShouldOpenConnection() {
            // Open
            mDBConnection.Open();             
            Assert.Equal(System.Data.ConnectionState.Open, mDBConnection.Connection.State);
            // Close
            mDBConnection.Close();
            Assert.Equal(System.Data.ConnectionState.Closed, mDBConnection.Connection.State);
        }

        //[Fact]
        //public void Close_ShouldCloseConnection()
        //{
        //     // ... setup your mocks and test data here ...

        //    // Act
        //    dbConnection.Close();

        //    // Assert
        //    // ... assert that the connection is closed ...
        //}

        //[Fact]
        //public void ExecuteNonQuery_ShouldReturnExpectedResult()
        //{
        //    // Arrange
        //    var dbConnection = new DBConnection();
        //    // ... setup your mocks and test data here ...

        //    // Act
        //    var result = dbConnection.ExecuteNonQuery("SQL command");

        //    // Assert
        //    // ... assert that the result is as expected ...
        //}

        //[Fact]
        //public void ExecuteScalar_ShouldReturnExpectedResult()
        //{
        //    // Arrange
        //    var dbConnection = new DBConnection();
        //    // ... setup your mocks and test data here ...

        //    // Act
        //    var result = dbConnection.ExecuteScalar<int>("SQL command");

        //    // Assert
        //    // ... assert that the result is as expected ...
        //}

        //[Fact]
        //public void ExecuteReader_ShouldReturnExpectedResult()
        //{
        //    // Arrange
        //    var dbConnection = new DBConnection();
        //    // ... setup your mocks and test data here ...

        //    // Act
        //    var result = dbConnection.ExecuteReader("SQL command");

        //    // Assert
        //    // ... assert that the result is as expected ...
        //}

        //[Fact]
        //public void BeginTransAndCommitTrans_ShouldStartAndCommitTransaction()
        //{
        //    // Arrange
        //    var dbConnection = new DBConnection();
        //    // ... setup your mocks and test data here ...

        //    // Act
        //    dbConnection.BeginTrans();
        //    dbConnection.CommitTrans();

        //    // Assert
        //    // ... assert that the transaction was started and committed ...
        //}
    }
}
