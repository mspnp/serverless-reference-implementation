// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import React from "react"
import { 
  Text,
  Stack } from "office-ui-fabric-react"
import { FontSizes } from '@uifabric/fluent-theme/lib/fluent/FluentType';

const titleStyle = { root: { fontSize: FontSizes.size42 } }
const descStyle = { root: { fontSize: FontSizes.size18 } }

export default () => (
      <>
        <Stack
        horizontalAlign="center"
        verticalAlign="center"
        verticalFill
        styles={{
          root: {
            width: "960px",
            margin: "0 auto",
            textAlign: "center",
            color: "#605e5c",
          },
        }}
        gap={15}>
          <Text styles={titleStyle}>404</Text>
          <Text styles={descStyle}>The page you requested cannot be found.</Text>
        </Stack>
      </>
  )