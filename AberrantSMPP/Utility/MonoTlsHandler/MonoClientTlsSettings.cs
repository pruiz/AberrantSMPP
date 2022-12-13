// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Handlers.Tls
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;

    public sealed class MonoClientTlsSettings : TlsSettings
    {
        public MonoClientTlsSettings(string targetHost)
            : this(targetHost, new List<X509Certificate>())
        {
        }

        public MonoClientTlsSettings(string targetHost, List<X509Certificate> certificates)
            : this(false, certificates, targetHost)
        {
        }

        public MonoClientTlsSettings(bool checkCertificateRevocation, List<X509Certificate> certificates, string targetHost)
            : this(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, checkCertificateRevocation, certificates, targetHost)
        {
        }

        public MonoClientTlsSettings(SslProtocols enabledProtocols, bool checkCertificateRevocation, List<X509Certificate> certificates, string targetHost)
            :base(enabledProtocols, checkCertificateRevocation)
        {
            this.X509CertificateCollection = new X509CertificateCollection(certificates.ToArray());
            this.TargetHost = targetHost;
            this.Certificates = certificates.AsReadOnly();
        }

        internal X509CertificateCollection X509CertificateCollection { get; set; }

        public IReadOnlyCollection<X509Certificate> Certificates { get; }

        public string TargetHost { get; }
    }
}