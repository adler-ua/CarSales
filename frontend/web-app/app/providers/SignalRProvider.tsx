'use client'

import { useAuctionStore } from '@/hooks/useAuctionStore';
import { useBidStore } from '@/hooks/useBidStore';
import { Bid } from '@/types';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
import React, { ReactNode, useEffect, useState } from 'react'

type Props = {
    children: ReactNode
}

export default function SignalRProvider({children}: Props) {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const setCurrentPrice = useAuctionStore(state => state.setCurrentPrice);
  const addBid = useBidStore(state => state.addBid);

  useEffect(()=>{
    const newConnection = new HubConnectionBuilder()
        .withUrl('http://localhost:6001/notifications')
        .withAutomaticReconnect()
        .build();

    setConnection(newConnection);
  }, []); // add empty dependencies in order for this to be executed only once.

  useEffect(() => {
    if(connection) {
        connection.start()
            .then(() => {
                console.log('Connected to notification hub');

                connection.on('BidPlaced', (bid: Bid) => {
                    if(bid.bidStatus.includes('Accepted')) {
                        setCurrentPrice(bid.auctionId, bid.amount);
                    }
                    addBid(bid);
                })
            }).catch(error => console.log(error));
    }
    //need to close the connection when the component is disposed:
    return () => {
        connection?.stop();
    }
  }, [connection, setCurrentPrice])
  
  return (
    children
  )
}
