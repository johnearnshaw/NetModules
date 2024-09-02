﻿/*
    The MIT License (MIT)

    Copyright (c) 2019 John Earnshaw.
    Repository Url: https://github.com/johnearnshaw/netmodules/

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
 */

using System.Threading;

namespace NetModules.Interfaces
{
    /// <summary>
    /// A basic interface that allows a <see cref="System.Threading.CancellationToken"/> to be set, allowing the <see cref="System.Threading.CancellationToken"/>
    /// to be monitored by a thread, task, or process. This interface is implemented in <see cref="CancellableEvent{I, O}"/>.
    /// </summary>
    public interface ICancellable
    {
        /// <summary>
        /// The <see cref="System.Threading.CancellationToken"/> that can be monitored for cancellation notification requests
        /// from within your <see cref="CancellableEvent{I, O}"/> handler. This interface is implemented in <see cref="CancellableEvent{I, O}"/>.
        /// </summary>
        CancellationToken CancellationToken { get;}

        /// <summary>
        /// Allows you to set a <see cref="System.Threading.CancellationToken"/> to be monitored for cancellation notification requests.
        /// Keep in mind that a CancellationToken is entirely cooperative and if not monitored for cancellation within a method will do
        /// nothing.
        /// </summary>
        void SetCancelToken(CancellationToken cancellationToken);
    }
}
