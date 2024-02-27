'use client'
import { useParamsStore } from '@/hooks/useParamsStore'
import React from 'react'
import { AiOutlineCar } from 'react-icons/ai'
import {usePathname, useRouter} from 'next/navigation'

export default function Logo() {
    const router = useRouter();
    const pathName = usePathname();
    const reset = useParamsStore(state => state.reset);

    function doReset() {
        if(pathName !== '/') router.push('/');
        reset();
    }

    return (
        <div className='cursor-pointer flex items-center gap-2 text-3xl font-semibold text-red-500'
            onClick={doReset}>
            <AiOutlineCar size={34} />
            <div>Car Sales</div>
        </div>
    )
}
