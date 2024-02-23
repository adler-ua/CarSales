'use client'

import React, { useEffect, useState } from 'react'
import AuctionCard from './AuctionCard';
import { Auction } from '@/types';
import AppPagination from '../components/AppPagination';
import { getData } from '../actions/auctionActions';
import Filters from './Filters';

export default /*async*/ function Listings() {
  /* using local state: */
  const [auctions, setAuctions] = useState<Auction[]>([]);
  const [pageCount, setPageCount] = useState(0);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(4);

  useEffect(() => {
    getData(pageNumber, pageSize).then(data => { 
      setAuctions(data.results);
      setPageCount(data.pageCount);
      //setPageNumber(pageNumber); // called when an event pageChanged is triggered
    })
  }, /* dependencies for the method to be called: */ [pageNumber, pageSize]) /* whenever pageNumber changes -- useEffect gets called again */

  if(auctions.length === 0) return <h3>Loading...</h3>

  return (
    <>
      <Filters pageSize={pageSize} setPageSize={setPageSize}/>
      <div className='grid grid-cols-4 gap-6'>
          {auctions.map(auction => (
              <AuctionCard auction={auction} key={auction.id} />
          ))}
      </div>
      <div className='flex justify-center mt-4'>
        <AppPagination pageChanged={setPageNumber} currentPage={pageNumber} pageCount={pageCount}/>
      </div>
    </>
  )
}
