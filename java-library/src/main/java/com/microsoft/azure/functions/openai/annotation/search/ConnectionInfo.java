/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for
 * license information.
 */
package com.microsoft.azure.functions.openai.annotation.search;

/**
 * Connection info object.
 */
public class ConnectionInfo {
    /**
     * The name of an app setting or environment variable which contains a connection string value.
     */
    private String connectionName;

    /**
     * The name of the collection or table to search or store.
     */
    private String collectionName;

    /**
     * Credentials for authenticating with the search provider.
     * The name of the app setting or environment variable containing the required credentials
     * for authenticating with the search provider. See the documentation for the search provider
     * extension to know what format the underlying credential value requires.
     */
    private String credentials;

    /**
     * Constructor for creating a ConnectionInfo object with the given parameters.
     * @param connectionName The name of an app setting or environment variable which contains a connection string value.
     * @param collectionName The name of the collection or table to search.
     * @param credentials The credentials for authenticating with the search provider.
     */
    public ConnectionInfo(String connectionName, String collectionName, String credentials) {
        this.connectionName = connectionName;
        this.collectionName = collectionName;
        this.credentials = credentials;
    }

    /**
     * Gets the connection name.
     * @return The name of an app setting or environment variable which contains a connection string value.
     */
    public String getConnectionName() {
        return connectionName;
    }

    /**
     * Sets the connection name.
     * @param connectionName The name of an app setting or environment variable which contains a connection string value.
     */
    public void setConnectionName(String connectionName) {
        this.connectionName = connectionName;
    }

    /**
     * Gets the collection name.
     * @return The name of the collection or table to search.
     */
    public String getCollectionName() {
        return collectionName;
    }

    /**
     * Sets the collection name.
     * @param collectionName The name of the collection or table to search.
     */
    public void setCollectionName(String collectionName) {
        this.collectionName = collectionName;
    }

    /**
     * Gets the credentials.
     * @return The credentials for authenticating with the search provider.
     */
    public String getCredentials() {
        return credentials;
    }

    /**
     * Sets the credentials.
     * @param credentials The credentials for authenticating with the search provider.
     */
    public void setCredentials(String credentials) {
        this.credentials = credentials;
    }
}
