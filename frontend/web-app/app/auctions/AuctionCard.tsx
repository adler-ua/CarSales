import Image from 'next/image'
import React from 'react'

type Props = {
    auction: any
}

export default function AuctionCard({auction}: Props) {
  return (
    <a href='#'>
        <div className='w-full bg-gray-200 aspect-w-16 aspect-h-10 rounded-lg overflow-hidden'>
            <Image 
                src={auction.imageUrl} 
                alt='image'
                fill
                className='object-cover'
            />
        </div>
    </a>
  )
}
