import { getSession } from 'next-auth/react'
import React from 'react'
import Heading from '../components/Heading'
import { getServerSession } from 'next-auth';

export default async function Session() {
  const session = await getServerSession();
  return (
    <div>
      <Heading title='Session Dashboard' />

      <div className='bg-blue-200 border-2 border-blue-500'>
        <h3 className='text-lg'>Session data</h3>
        <pre>{JSON.stringify(session, null, 2)}</pre>
      </div>
    </div>
  )
}
