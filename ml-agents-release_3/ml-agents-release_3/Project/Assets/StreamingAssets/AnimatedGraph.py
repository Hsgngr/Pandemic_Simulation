# -*- coding: utf-8 -*-
"""
Created on Tue Jul 28 17:48:26 2020

@author: Ege
"""
# Importing the libraries
import seaborn as sns
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from sklearn.cluster import KMeans

# Styling
sns.set()
blue = sns.color_palette("muted", 1)


# Importing the dataset
dataset=pd.read_csv('export.csv', sep=';')
dataset['Time'] = dataset['Time'].str.replace(',','.')

def exportAnimation(data,number_of_frame,cluster_number):
    for i in range(0,5212,100):      
        dataset=data.iloc[:i+100,:] 
        x=data['HealthyCount']
        df = pd.DataFrame(data = dataset['Time'])
        df.insert(1,'HealthyCount',x)
        
        
        kmeans = KMeans(n_clusters=cluster_number)
        kmeans.fit(df)
        y_kmeans = kmeans.predict(df)
        centers = kmeans.cluster_centers_
        centers=np.around(centers,decimals=1)
        
        centers = pd.DataFrame(data= centers)
        centers.rename(columns={0:'time',1:'HealthyCount'}, inplace=True)
        centers.sort_values(by=['time'], inplace=True)
        centers= centers.to_numpy()    
        
        x=centers[:,0]
        y=centers[:,1]
        #######################################################################
        #Creating the Infection Graph
        x2=dataset['InfectedCount']
        
        # plot of 2 variables
        
        df2 = pd.DataFrame(data = dataset['Time'])
        df2.insert(1,'InfectedCount',x2)
        
        #By using kmeans grouping the point to decrease noise.
        
        kmeans = KMeans(n_clusters=cluster_number)
        kmeans.fit(df2)
        y_kmeans = kmeans.predict(df2)
        centers = kmeans.cluster_centers_
        
        centers=np.around(centers,decimals=1)
        
        #plt.scatter(centers[:, 0], centers[:, 1], c='black');
        centers = pd.DataFrame(data= centers)
        centers.rename(columns={0:'time',1:'InfectedCounts'}, inplace=True)
        centers.sort_values(by=['time'], inplace=True)
        centers= centers.to_numpy()
        #plt.plot(centers[:,0],centers[:,1])
        
        x2=centers[:,0]
        y2=centers[:,1]
        #######################################################################
        #Creating the Infection Graph
        x3=dataset['RecoveredCount']
        
        # plot of 2 variables
        
        df3 = pd.DataFrame(data = dataset['Time'])
        df3.insert(1,'RecoveredCount',x3)
        
        #By using kmeans grouping the point to decrease noise.
        
        kmeans = KMeans(n_clusters=cluster_number)
        kmeans.fit(df3)
        y_kmeans = kmeans.predict(df3)
        centers = kmeans.cluster_centers_
        
        centers=np.around(centers,decimals=1)
        
        #plt.scatter(centers[:, 0], centers[:, 1], c='black');
        centers = pd.DataFrame(data= centers)
        centers.rename(columns={0:'time',1:'RecoveredCount'}, inplace=True)
        centers.sort_values(by=['time'], inplace=True)
        centers= centers.to_numpy()
        #plt.plot(centers[:,0],centers[:,1])
        
        x3=centers[:,0]
        y3=centers[:,1]
        #######################################################################
        fig= plt.figure()
        plt.rcParams["font.family"] = "serif"
        plt.plot(x,y,alpha =1,linewidth=3, label='Susceptible')
        plt.plot(x2,y2, alpha= 1,linewidth=3, label ='Infected')
        plt.plot(x3,y3,alpha =1,linewidth=3, label ='Recovered')
        
        fig.suptitle('SIR Model', fontsize=16)
        plt.xlabel('Time (s)', fontsize=12)
        plt.ylabel('#Individuals', fontsize=12)
        
        if i < 4800:
            plt.legend(framealpha=1, frameon=True, loc='center left');
        else:
            plt.legend(framealpha=1, frameon=True);
        
        plt.savefig("Graph" + str(i) +".png", format="PNG")
        plt.close()
        
        if cluster_number <=99:
            cluster_number = cluster_number+10
        
exportAnimation(dataset,500,10)
