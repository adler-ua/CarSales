import { Button, ButtonGroup } from 'flowbite-react';
import React from 'react'

type Props={
    pageSize: number
    setPageSize: (size: number) => void;
}

const pageSizeButtons = [4, 8, 12];

export default function Filters({pageSize, setPageSize}: Props) {
  return (
    <div className='flex justify-between items-center mb-4'>
        <div>
            <span className='uppercase test-sm text-gray-500 mr-2'>Page size</span>
            <ButtonGroup>
                {pageSizeButtons.map((value, i/* index we are looping over, and use as a unique key */) => /*immediately return*/
                    (
                        <Button key={i} 
                            onClick={()=>setPageSize(value)}
                            color={`${pageSize === value ? 'red' : 'gray'}`}
                        >
                            {value}
                        </Button>                                            
                    ))}
            </ButtonGroup>
        </div>
    </div>
  )
}
