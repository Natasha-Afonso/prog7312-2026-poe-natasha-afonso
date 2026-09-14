# Use the Microsoft SQL Server image as the base
FROM mcr.microsoft.com/mssql/server:2022-latest

# Set environment variables
ENV ACCEPT_EULA=Y 
ENV MSSQL_PID=Express 
ENV MSSQL_SA_PASSWORD=Password@1!

# Switch to root to ensure we can create directories
USER root
RUN mkdir -p /app/init-scripts && chown mssql /app/init-scripts

# Copy the initialization script (Must be in the same folder as this Dockerfile)
COPY ./MedicDB/init-database.sql /app/init-scripts/

# Ensure mssql-tools are in the PATH
ENV PATH="$PATH:/opt/mssql-tools18/bin"

# Revert to default SQL Server user
USER mssql

# Healthcheck using the updated tools path and -C (trust server certificate)
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=5 \
  CMD /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" || exit 1

# Start SQL Server and run the init script
# Added -C to sqlcmd to handle SSL certificate trust in newer images
CMD /bin/bash -c "/opt/mssql/bin/sqlservr & \
  sleep 25 && \
  if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '$MSSQL_SA_PASSWORD' -C -Q 'SELECT name FROM sys.databases WHERE name = \"Medic\"' | grep -q Medic; then \
    echo 'Database already exists'; \
  else \
    echo 'Initializing database'; \
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '$MSSQL_SA_PASSWORD' -C -d master -i /app/init-scripts/init-database.sql; \
  fi && wait"